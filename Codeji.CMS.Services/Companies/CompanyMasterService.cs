

using System.Collections;
using System.Linq.Expressions;
using AngleSharp.Common;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.DTO.Company.CustomAttribute;
using Codeji.CMS.DTO.Company.Department;
using Codeji.CMS.DTO.Company.JobTitle;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Companies;

public class CompanyMasterService : ICompanyMasterService
{
    private readonly IMongoDbRepository<Department> _departmentRepository;
    readonly IMapper _mapper;
    readonly IMongoDbRepository<Module> _moduleRepository;
    readonly IMongoDbRepository<ModulePermission> _modulePermissionRepository;
    readonly IMongoDbRepository<Permission> _permissionRepository;

    readonly IMongoDbRepository<RolePermission> _rolePermissionRepository;
    readonly IHttpContextAccessor _httpContextAccessor;
    readonly IRoleService _roleService;
    readonly IMongoDbRepository<JobTitles> _jobTitleRepository;
    readonly IMongoDbRepository<CustomAttribute> _customAttributeRepository;

    public CompanyMasterService(IRoleService roleService, IMongoDbRepository<CustomAttribute> customAttributeRepository, IMongoDbRepository<Department> departmentRepository, IMapper mapper, IMongoDbRepository<Module> moduleRepository, IMongoDbRepository<ModulePermission> modulePermissionRepository, IMongoDbRepository<Permission> permissionRepository, IMongoDbRepository<RolePermission> rolePermissionRepository, IMongoDbRepository<JobTitles> jobTitleRepository, IHttpContextAccessor httpContextAccessor)
    {
        _departmentRepository = departmentRepository;
        _mapper = mapper;
        _moduleRepository = moduleRepository;
        _modulePermissionRepository = modulePermissionRepository;
        _permissionRepository = permissionRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _httpContextAccessor = httpContextAccessor;
        _roleService = roleService;
        _jobTitleRepository = jobTitleRepository;
        _customAttributeRepository = customAttributeRepository;
    }

    public async Task<Result> UpdateDepartments(List<DepartmentRequestDto> departmentList, string userId)
    {
        List<Department> departments = new();
        _mapper.Map(departmentList, departments);
        Result result = new();
        foreach (Department department in departments)
        {
            if (string.IsNullOrEmpty(department.DepartmentId))
            {
                department.CreatedBy = userId;
                department.CreatedDate = DateTime.UtcNow;
                result = await _departmentRepository.AddOne(department);
            }
            else
            {
                Expression<Func<Department, bool>> whereCondition = x => x.DepartmentId == department.DepartmentId;
                result = await _departmentRepository.UpdateMany(whereCondition, Builders<Department>.Update.Set(x => x.UpdatedBy, userId).Set(x => x.UpdatedDate, DateTime.UtcNow).Set(x => x.Titles, department.Titles).Set(x => x.IsActive, department.IsActive));
            }
        }
        return result;
    }

    public async Task<Result<DepartmentResponseDto>> GetDepartmentList(bool? isActive)
    {
        Result<DepartmentResponseDto> result = new() { Success = false };
        IEnumerable<Department> departments = await _departmentRepository.GetAll(x => x.IsDeleted == false);
        if (isActive.HasValue)
        {
            departments = departments.Where(x => x.IsActive == isActive.Value);
        }

        if (departments == null || !departments.Any()) return result;
        var data = departments.Select(d => new DepartmentResponseDto()
        {
            DepartmentId = d.DepartmentId,
            IsActive = d.IsActive,
            Titles = d.Titles.ToDictionary(keySelector: dt => dt.Language, elementSelector: dt => dt.Label),
        });
        result.MethodResults = data.ToList();
        result.Success = true;
        result.TotalRecords = data.Count();
        return result;
    }
    public async Task<bool> DeleteDepartment(string departmentId)
    {
        Expression<Func<Department, bool>> whereCondition = x => x.DepartmentId == departmentId;
        Department? department = await _departmentRepository.FirstOrDefault(whereCondition);
        if (department is null)
        {
            return false;
        }
        department.IsDeleted = true;
        var result = await _departmentRepository.Update(whereCondition, department);
        return result.Success;
    }
    public async Task<Result<string[]>> UpdateModuleAccess(string moduleId)
    {
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        Module? module = await _moduleRepository.FirstOrDefault(x => x._id == moduleId);
        if (module is null)
        {
            return new Result<string[]>()
            {
                Success = false,
            };
        }
        List<ModulePermission> modulesPermission = (await _modulePermissionRepository.GetAll(x => x.ModuleId == module.ModuleId)).ToList();
        int[] modulePermissionId = modulesPermission.Select(x => x.ModulePermissionId).ToArray();
        IEnumerable<RolePermission> rolePermissions = await _rolePermissionRepository.GetAll(x => x.CompanyId == companyId && modulePermissionId.Contains(x.ModulePermissionId));
        if (!rolePermissions.Any())
        {
            return new Result<string[]>()
            {
                Success = false,
            };
        }
        string roleId = CurrentContext.UserRoleId(_httpContextAccessor);
        bool hasAccess = rolePermissions.Take(1).ToList()[0].IsAccessible;
        Expression<Func<RolePermission, bool>> whereCondition = x => x.CompanyId.Equals(companyId) && modulePermissionId.Contains(x.ModulePermissionId);
        Result result = await _rolePermissionRepository.UpdateMany(whereCondition, Builders<RolePermission>.Update.Set(x => x.IsAccessible, !hasAccess));
        string[] updatedPermissions = await _roleService.GetRolePermissionOfuser(roleId);
        return new Result<string[]>()
        {
            Success = true,
            MethodResult = updatedPermissions
        };
    }
    public async Task<List<AllModuleDetailsResponseModel>> GetAllModulesDetails(string companyId)
    {
        int[] excludedModuleIds = [11];
        var allModules = await _moduleRepository.GetAll(x => !excludedModuleIds.Contains(x.ModuleId));
        var allModulePermissions = await _modulePermissionRepository.GetAll();
        var allRolePermission = await _rolePermissionRepository.GetAll(x => x.CompanyId == companyId);

        var queryResult = from module in allModules
                          join modulePermission in allModulePermissions on module.ModuleId equals modulePermission.ModuleId into modulePermissionGroup
                          from modulePermission in modulePermissionGroup.Take(1)
                          join rolePermission in allRolePermission on modulePermission.ModulePermissionId equals rolePermission.ModulePermissionId into roleGroup
                          from rolePermission in roleGroup.Take(1)
                          select new AllModuleDetailsResponseModel
                          {
                              ModuleId = module._id,
                              ModuleName = module.ModuleName,
                              ModuleConstant = module.ModuleConstant,
                              IsAccessible = rolePermission.IsAccessible,
                          };

        return queryResult.ToList();
    }

    // Company Job Titles Services
    public async Task<Result<JobTitleResponseDto>> GetJobTitles(bool? isActive)
    {
        Result<JobTitleResponseDto> result = new() { Success = false };
        IEnumerable<JobTitles> jobTitles = await _jobTitleRepository.GetAll(x => x.IsDeleted == false);
        if (isActive.HasValue)
        {
            jobTitles = jobTitles.Where(x => x.IsActive == isActive.Value);
        }

        if (jobTitles == null || !jobTitles.Any()) return result;
        var data = jobTitles.Select(jt => new JobTitleResponseDto()
        {
            JobTitleId = jt.JobTitleId,
            IsActive = jt.IsActive,
            Titles = jt.Titles.ToDictionary(keySelector: jt => jt.Language, elementSelector: jt => jt.Label),
        });
        result.MethodResults = data.ToList();
        result.Success = true;
        result.TotalRecords = data.Count();
        return result;
    }

    public async Task<Result> AddUpdateJobTitle(List<JobTitleRequestDto> jobTitleList, string userId)
    {
        List<JobTitles> jobTitles = new();
        _mapper.Map(jobTitleList, jobTitles);
        Result result = new();
        foreach (JobTitles title in jobTitles)
        {
            if (string.IsNullOrEmpty(title.JobTitleId))
            {
                title.CreatedBy = userId;
                title.CreatedDate = DateTime.UtcNow;
                result = await _jobTitleRepository.AddOne(title);
            }
            else
            {
                Expression<Func<JobTitles, bool>> whereCondition = x => x.JobTitleId == title.JobTitleId;
                result = await _jobTitleRepository.UpdateMany(whereCondition, Builders<JobTitles>.Update.Set(x => x.UpdatedBy, userId).Set(x => x.UpdatedDate, DateTime.UtcNow).Set(x => x.Titles, title.Titles).Set(x => x.IsActive, title.IsActive));
            }
        }
        return result;
    }

    public async Task<bool> DeleteJobTitle(string jobTitleId)
    {
        Expression<Func<JobTitles, bool>> whereCondition = jt => jt.JobTitleId == jobTitleId;
        JobTitles? jobTitle = await _jobTitleRepository.FirstOrDefault(whereCondition);
        if (jobTitle is null)
        {
            return false;
        }
        jobTitle.IsDeleted = true;
        var result = await _jobTitleRepository.Update(whereCondition, jobTitle);
        return result.Success;
    }

    // Custom Attributes Services
    public async Task<CustomAttributeResponseDto> CreateCustomAttribute(string userId)
    {
        CustomAttribute customAttribute = new()
        {
            CustomAttributeId = Guid.NewGuid().ToString(),
            CustomAttributeName = null,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow,
        };
        var result = await _customAttributeRepository.AddOne(customAttribute);
        // if (!result.Success)
        // {
        //     return null;
        // }
        CustomAttributeResponseDto responseDto = new()
        {
            CustomAttributeId = customAttribute.CustomAttributeId,
            CustomAttributeName = customAttribute.CustomAttributeName
        };
        return responseDto;
    }
}

