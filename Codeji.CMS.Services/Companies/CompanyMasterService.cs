

using System.Collections;
using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Interface;
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

    public CompanyMasterService(IMongoDbRepository<Department> departmentRepository, IMapper mapper, IMongoDbRepository<Module> moduleRepository, IMongoDbRepository<ModulePermission> modulePermissionRepository, IMongoDbRepository<Permission> permissionRepository, IMongoDbRepository<RolePermission> rolePermissionRepository, IHttpContextAccessor httpContextAccessor)
    {
        _departmentRepository = departmentRepository;
        _mapper = mapper;
        _moduleRepository = moduleRepository;
        _modulePermissionRepository = modulePermissionRepository;
        _permissionRepository = permissionRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _httpContextAccessor = httpContextAccessor;
    }
    public async Task<Result> AddEditDepartment(DepartmentDTO model)
    {
        Result result = new();
        Expression<Func<Department, bool>> whereCondition = x => x.DepartmentId == model.DepartmentId;
        Department dep = _departmentRepository.FirstOrDefault(whereCondition)?.Result ?? new();
        if (string.IsNullOrEmpty(dep.DepartmentId))
        {
            dep.IsActive = model.IsActive;
            dep.Titles = model.Titles;

            result = await _departmentRepository.AddOne(dep);
            if (result.Success) result.Message = "Department Added Successfully";
        }
        else
        {
            dep.IsActive = model.IsActive;
            dep.Titles = model.Titles;
            result = await _departmentRepository.Update(whereCondition, dep);
            if (result.Success) result.Message = "Department Edited Successfully";
        }
        return result;
    }

    public async Task<Result<DepartmentDTO>> GetDepartmentList()
    {
        IEnumerable<Department> departments = await _departmentRepository.GetAll(x => x.IsDeleted == false);
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

    public async Task<Result> UpdateModuleAccess(string moduleId, bool hasAccess)
    {
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        Module? module = await _moduleRepository.FirstOrDefault(x => x._id == moduleId);
        if (module is null)
        {
            return new Result()
            {
                Success = false,
                Message = "Failed To Updated"
            };
        }
        List<ModulePermission> modulesPermission = (await _modulePermissionRepository.GetAll(x => x.ModuleId == module.ModuleId)).ToList();
        int[] modulePermissionId = modulesPermission.Select(x => x.ModulePermissionId).ToArray();
        IEnumerable<RolePermission> rolePermissions = await _rolePermissionRepository.GetAll(x => x.CompanyId == companyId && modulePermissionId.Contains(x.ModulePermissionId));

        Expression<Func<RolePermission, bool>> whereCondition = x => x.CompanyId.Equals(companyId) && modulePermissionId.Contains(x.ModulePermissionId);
        Result result = await _rolePermissionRepository.UpdateMany(whereCondition, Builders<RolePermission>.Update.Set(x => x.IsAccessible, hasAccess));
        return result;
    }

    public async Task<List<ModuleDTO>> GetAllModule(string companyId)
    {
        IEnumerable<Module> modules = await _moduleRepository.GetAll();
        IEnumerable<ModulePermission> modulePermissions = await _modulePermissionRepository.GetAll();
        IEnumerable<RolePermission> rolePermissions = await _rolePermissionRepository.GetAll(x => x.CompanyId == companyId);
    }
}

