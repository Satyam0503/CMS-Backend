

using System.Collections;
using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
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

    public CompanyMasterService(IRoleService roleService, IMongoDbRepository<Department> departmentRepository, IMapper mapper, IMongoDbRepository<Module> moduleRepository, IMongoDbRepository<ModulePermission> modulePermissionRepository, IMongoDbRepository<Permission> permissionRepository, IMongoDbRepository<RolePermission> rolePermissionRepository, IHttpContextAccessor httpContextAccessor)
    {
        _departmentRepository = departmentRepository;
        _mapper = mapper;
        _moduleRepository = moduleRepository;
        _modulePermissionRepository = modulePermissionRepository;
        _permissionRepository = permissionRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _httpContextAccessor = httpContextAccessor;
        _roleService = roleService;
    }

    public async Task<Result> UpdateDepartments(List<DepartmentDTO> departmentList, string userId)
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

    public async Task<Result<DepartmentDTO>> GetDepartmentList(bool? isActive)
    {
        IEnumerable<Department> departments = await _departmentRepository.GetAll(x => x.IsDeleted == false);
        if (isActive.HasValue)
        {
            departments = departments.Where(x => x.IsActive == isActive.Value);
        }

        if (departments == null || !departments.Any())
        {
            return new Result<DepartmentDTO>()
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = "No Department Found"
            };
        }

        List<DepartmentDTO> data = _mapper.Map<List<DepartmentDTO>>(departments);
        return new Result<DepartmentDTO>()
        {
            MethodResults = data.ToList(),
            TotalRecords = data.Count,
            Success = true,
            StatusCode = 200,
            Message = "List Of Departments",
        };
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
        await _departmentRepository.Update(whereCondition, department);
        return true;
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
            Message = "Successfully Updated",
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
}

