

using System.Collections;
using System.Linq.Expressions;
using AngleSharp.Common;
using MapsterMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.DTO.Company.CustomAttribute;
using Codeji.CMS.DTO.Company.Department;
using Codeji.CMS.DTO.Company.JobTitle;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
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
    readonly IMongoDbRepository<CustomAttributeValue> _customAttributeValueRepository;
    readonly IMongoDbRepository<EmpUser> _employeeRepository;

    public CompanyMasterService(IRoleService roleService, IMongoDbRepository<CustomAttribute> customAttributeRepository, IMongoDbRepository<CustomAttributeValue> customAttributeValueRepository, IMongoDbRepository<Department> departmentRepository, IMapper mapper, IMongoDbRepository<Module> moduleRepository, IMongoDbRepository<ModulePermission> modulePermissionRepository, IMongoDbRepository<Permission> permissionRepository, IMongoDbRepository<RolePermission> rolePermissionRepository, IMongoDbRepository<JobTitles> jobTitleRepository, IMongoDbRepository<EmpUser> employeeRepository, IHttpContextAccessor httpContextAccessor)
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
        _customAttributeValueRepository = customAttributeValueRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<Result> UpdateDepartments(List<DepartmentRequestDto> departmentList, string userId)
    {
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        List<Department> departments = new();
        _mapper.Map(departmentList, departments);
        Result result = new() { Success = true, StatusCode = StatusCodes.Status200OK };
        foreach (Department department in departments)
        {
            if (string.IsNullOrEmpty(department.DepartmentId))
            {
                department.CompanyId = companyId;
                department.CreatedBy = userId;
                department.CreatedDate = DateTime.UtcNow;
                result = await _departmentRepository.AddOne(department);
            }
            else
            {
                Expression<Func<Department, bool>> whereCondition = x => x.DepartmentId == department.DepartmentId && x.CompanyId == companyId && !x.IsDeleted;
                result = await _departmentRepository.UpdateMany(whereCondition, Builders<Department>.Update.Set(x => x.UpdatedBy, userId).Set(x => x.UpdatedDate, DateTime.UtcNow).Set(x => x.Titles, department.Titles).Set(x => x.IsActive, department.IsActive));
            }
        }
        return result;
    }

    public async Task<Result<DepartmentResponseDto>> GetDepartmentList(bool? isActive)
    {
        Result<DepartmentResponseDto> result = new() { Success = false };
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        IEnumerable<Department> departments = await _departmentRepository.GetAll(x => x.CompanyId == companyId && !x.IsDeleted);
        if (isActive.HasValue)
        {
            departments = departments.Where(x => x.IsActive == isActive.Value);
        }

        if (departments == null || !departments.Any()) return result;
        var data = departments.Select(d => new DepartmentResponseDto()
        {
            DepartmentId = d.DepartmentId,
            IsActive = d.IsActive,
            Titles = DefaultCompanySeeds.GetTitlesWithKnownTranslations(d.Titles),
        });
        result.MethodResults = data.ToList();
        result.Success = true;
        result.TotalRecords = data.Count();
        return result;
    }
    public async Task<bool> DeleteDepartment(string departmentId)
    {
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        Expression<Func<Department, bool>> whereCondition = x => x.DepartmentId == departmentId && x.CompanyId == companyId && !x.IsDeleted;
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
        Expression<Func<RolePermission, bool>> whereCondition = x => x.CompanyId == companyId && modulePermissionId.Contains(x.ModulePermissionId);
        IEnumerable<RolePermission> rolePermissions = await _rolePermissionRepository.GetAll(whereCondition);
        if (!rolePermissions.Any())
        {
            return new Result<string[]>()
            {
                Success = false,
            };
        }
        string roleId = CurrentContext.UserRoleId(_httpContextAccessor);
        bool hasAccess = rolePermissions.Take(1).ToList()[0].IsAccessible;
        Result result = await _rolePermissionRepository.UpdateMany(whereCondition, Builders<RolePermission>.Update.Set(x => x.IsAccessible, !hasAccess));
        string[] updatedPermissions = await _roleService.GetRolePermissionOfuser(roleId, companyId);
        return new Result<string[]>()
        {
            Success = true,
            MethodResult = updatedPermissions
        };
    }
    public async Task<List<AllModuleDetailsResponseModel>> GetAllModulesDetails(string companyId)
    {
        var allModules = await _moduleRepository.GetAll();
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
    public async Task<Result<JobTitleResponseDto>> GetJobTitles(bool? isActive, string? departmentId)
    {
        Result<JobTitleResponseDto> result = new() { Success = false };
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        IEnumerable<JobTitles> jobTitles = await _jobTitleRepository.GetAll(x =>
            x.CompanyId == companyId && !x.IsDeleted &&
            (string.IsNullOrWhiteSpace(departmentId) || x.DepartmentId == departmentId));
        if (isActive.HasValue)
        {
            jobTitles = jobTitles.Where(x => x.IsActive == isActive.Value);
        }

        if (jobTitles == null || !jobTitles.Any()) return result;
        var data = jobTitles.Select(jt => new JobTitleResponseDto()
        {
            JobTitleId = jt.JobTitleId,
            DepartmentId = jt.DepartmentId,
            IsActive = jt.IsActive,
            Titles = DefaultCompanySeeds.GetTitlesWithKnownTranslations(jt.Titles),
        });
        result.MethodResults = data.ToList();
        result.Success = true;
        result.TotalRecords = data.Count();
        return result;
    }

    public async Task<Result> AddUpdateJobTitle(List<JobTitleRequestDto> jobTitleList, string userId)
    {
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        var validationErrors = new List<ValidationError>();
        var existingIds = jobTitleList.Where(x => !string.IsNullOrWhiteSpace(x.JobTitleId)).Select(x => x.JobTitleId!).ToHashSet();
        var existing = (await _jobTitleRepository.GetAll(x => x.CompanyId == companyId && existingIds.Contains(x.JobTitleId) && !x.IsDeleted)).ToDictionary(x => x.JobTitleId);
        foreach (JobTitleRequestDto request in jobTitleList)
        {
            // A legacy row that remains unchanged and unmapped must not prevent
            // administrators from mapping another row in the same bulk request.
            // New rows, cleared mappings, and changed mappings still require an
            // active same-company department.
            bool unchangedLegacyMapping = !string.IsNullOrWhiteSpace(request.JobTitleId)
                && existing.TryGetValue(request.JobTitleId, out var existingTitle)
                && string.IsNullOrWhiteSpace(existingTitle.DepartmentId)
                && string.IsNullOrWhiteSpace(request.DepartmentId);
            if (!unchangedLegacyMapping && (string.IsNullOrWhiteSpace(request.DepartmentId) || !await _departmentRepository.Exist(x =>
                x.DepartmentId == request.DepartmentId && x.CompanyId == companyId && x.IsActive && !x.IsDeleted)))
            {
                validationErrors.Add(new ValidationError { JobTitleId = request.JobTitleId, JobTitleName = EnglishTitle(request.Titles), Field = "departmentId", Message = "Select an active department." });
            }
            if (!string.IsNullOrWhiteSpace(request.JobTitleId) && !existing.ContainsKey(request.JobTitleId))
            {
                validationErrors.Add(new ValidationError { JobTitleId = request.JobTitleId, JobTitleName = EnglishTitle(request.Titles), Field = "jobTitleId", Message = "Job title is not available in the current company." });
            }
        }
        if (validationErrors.Count != 0)
            return new Result { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "Some job titles have invalid department mappings.", Errors = validationErrors };

        var jobTitles = new List<JobTitles>();
        foreach (JobTitleRequestDto request in jobTitleList)
        {
            if (string.IsNullOrWhiteSpace(request.JobTitleId))
            {
                jobTitles.Add(new JobTitles
                {
                    JobTitleId = string.Empty,
                    CompanyId = companyId,
                    DepartmentId = request.DepartmentId,
                    IsActive = request.IsActive,
                    Titles = request.Titles ?? [],
                    CreatedBy = userId,
                    CreatedDate = DateTime.UtcNow,
                });
                continue;
            }

            if (!existing.TryGetValue(request.JobTitleId, out var current))
            {
                continue;
            }

            var normalizedTitles = request.Titles ?? [];
            bool unchanged = current.IsActive == request.IsActive
                && current.DepartmentId == request.DepartmentId
                && current.Titles.OrderBy(x => x.Language).SequenceEqual(normalizedTitles.OrderBy(x => x.Language), new MultilingualTitleComparer());
            if (unchanged)
            {
                continue;
            }

            jobTitles.Add(new JobTitles
            {
                JobTitleId = request.JobTitleId,
                CompanyId = companyId,
                DepartmentId = request.DepartmentId,
                IsActive = request.IsActive,
                Titles = normalizedTitles,
                UpdatedBy = userId,
                UpdatedDate = DateTime.UtcNow,
            });
        }

        if (jobTitles.Count == 0)
        {
            return new Result { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "No valid job title changes were saved." };
        }

        Result result = new() { Success = true, StatusCode = StatusCodes.Status200OK };
        foreach (JobTitles title in jobTitles)
        {
            if (string.IsNullOrEmpty(title.JobTitleId))
            {
                result = await _jobTitleRepository.AddOne(title);
            }
            else
            {
                Expression<Func<JobTitles, bool>> whereCondition = x => x.JobTitleId == title.JobTitleId && x.CompanyId == companyId && !x.IsDeleted;
                result = await _jobTitleRepository.UpdateMany(whereCondition, Builders<JobTitles>.Update.Set(x => x.UpdatedBy, userId).Set(x => x.UpdatedDate, DateTime.UtcNow).Set(x => x.Titles, title.Titles).Set(x => x.DepartmentId, title.DepartmentId).Set(x => x.IsActive, title.IsActive));
            }

            if (!result.Success)
            {
                return result;
            }
        }

        return result;
    }

    public async Task<bool> DeleteJobTitle(string jobTitleId)
    {
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        Expression<Func<JobTitles, bool>> whereCondition = jt => jt.JobTitleId == jobTitleId && jt.CompanyId == companyId && !jt.IsDeleted;
        JobTitles? jobTitle = await _jobTitleRepository.FirstOrDefault(whereCondition);
        if (jobTitle is null)
        {
            return false;
        }
        // Preserve historical employee references. An unreferenced record can be
        // removed; a referenced one is safely retired instead.
        if (await _employeeRepository.Exist(e => e.CompanyId == companyId && e.JobRole == jobTitleId && !e.IsDeleted))
        {
            jobTitle.IsActive = false;
            jobTitle.UpdatedDate = DateTime.UtcNow;
            var deactivate = await _jobTitleRepository.Update(whereCondition, jobTitle);
            return deactivate.Success;
        }
        jobTitle.IsDeleted = true;
        var result = await _jobTitleRepository.Update(whereCondition, jobTitle);
        return result.Success;
    }

    private static string? EnglishTitle(IEnumerable<MultilingualModel>? titles) =>
        titles?.FirstOrDefault(x => string.Equals(x.Language, "en", StringComparison.OrdinalIgnoreCase))?.Label
        ?? titles?.FirstOrDefault()?.Label;

    private sealed class MultilingualTitleComparer : IEqualityComparer<MultilingualModel>
    {
        public bool Equals(MultilingualModel? x, MultilingualModel? y) => x?.Language == y?.Language && x?.Label == y?.Label;
        public int GetHashCode(MultilingualModel obj) => HashCode.Combine(obj.Language, obj.Label);
    }

    // Custom Attributes Services
    public async Task<CustomAttributeResponseDto?> CreateCustomAttribute(string companyId)
    {
        // get count of custom attribute 
        // const int MaxAllowedAttribute = 5;
        int totalAttribute = await _customAttributeRepository.Count(ca => ca.CompanyId == companyId);
        // if (totalAttribute >= MaxAllowedAttribute)
        // {
        //     return null;
        // }

        CustomAttribute customAttribute = new()
        {
            CustomAttributeId = Guid.NewGuid().ToString(),
            CompanyId = companyId,
            CustomAttributeTitle = [],
            CustomAttributeNumber = totalAttribute + 1
        };
        var result = await _customAttributeRepository.AddOne(customAttribute);
        if (!result.Success) return null;
        CustomAttributeResponseDto responseDto = new()
        {
            CustomAttributeId = customAttribute.CustomAttributeId,
            CustomAttributeTitle = customAttribute.CustomAttributeTitle.ToDictionary(keySelector: ca => ca.Language, elementSelector: ca => ca.Label),
            CustomAttributeNumber = customAttribute.CustomAttributeNumber,
        };
        return responseDto;
    }

    public async Task<List<CustomAttributeResponseDto>> GetAllCustomAttribute(string companyId)
    {
        IEnumerable<CustomAttribute> customAttributes = await _customAttributeRepository.GetAll(ca => ca.CompanyId == companyId);
        if (!customAttributes.Any()) return [];
        List<CustomAttributeResponseDto> dataList = [.. customAttributes.Select(ca => new CustomAttributeResponseDto()
        {
            CustomAttributeId = ca.CustomAttributeId,
            CustomAttributeTitle = ca.CustomAttributeTitle.ToDictionary(a => a.Language,a => a.Label),
            CustomAttributeNumber = ca.CustomAttributeNumber,
        })];
        return dataList;
    }

    public async Task<Result<CustomAttributeByIdResponseDto>> GetCustomAttributeById(string customAttributeId, string companyId, bool? active)
    {
        Result<CustomAttributeByIdResponseDto> result = new() { Success = false };
        CustomAttribute? customAttribute = await _customAttributeRepository.FirstOrDefault(ca => ca.CompanyId == companyId && ca.CustomAttributeId == customAttributeId);
        if (customAttribute == null) return result;
        IEnumerable<CustomAttributeValue> customAttributeValuesList = await _customAttributeValueRepository.GetAll(v => v.CompanyId == companyId && v.CustomAttributeId == customAttributeId);
        // Older value rows could be created without CompanyId. The parent attribute
        // has already been verified for this company, and CustomAttributeId is unique,
        // so these rows can be safely repaired and returned to the editor.
        var orphanedValues = await _customAttributeValueRepository.GetAll(
            v => v.CustomAttributeId == customAttributeId && string.IsNullOrWhiteSpace(v.CompanyId),
            withDefaultFilter: false);
        foreach (var orphanedValue in orphanedValues)
        {
            orphanedValue.CompanyId = companyId;
            await _customAttributeValueRepository.Update(
                Builders<CustomAttributeValue>.Filter.Eq(v => v.CustomAttributeValueId, orphanedValue.CustomAttributeValueId) &
                Builders<CustomAttributeValue>.Filter.Eq(v => v.CustomAttributeId, customAttributeId),
                orphanedValue);
        }
        customAttributeValuesList = customAttributeValuesList.Concat(orphanedValues);
        if (active.HasValue && active.Value)
        {
            customAttributeValuesList = customAttributeValuesList.Where(v => v.IsActive);
        }
        CustomAttributeByIdResponseDto data = new()
        {
            CustomAttributeId = customAttribute.CustomAttributeId,
            CustomAttributeTitle = customAttribute.CustomAttributeTitle.Count != 0 ? customAttribute.CustomAttributeTitle.ToDictionary(t => t.Language, t => t.Label) : null,
            CustomAttributeNumber = customAttribute.CustomAttributeNumber,
            CustomAttributeValues = customAttributeValuesList.Select(v => new CustomAttributeValueResponseDto()
            {
                CustomAttributeValueId = v.CustomAttributeValueId,
                IsActive = v.IsActive,
                Titles = v.Titles.ToDictionary(t => t.Language, t => t.Label)
            }).ToList()
        };
        result.MethodResult = data;
        result.Success = true;
        return result;
    }
    public async Task<Result> UpdateCustomAttribute(CustomAttributeRequestDto model, string companyId, string userId)
    {
        Result result = new() { Success = false };
        if (string.IsNullOrWhiteSpace(model.CustomAttributeId))
        {
            result.Message = "CUSTOM_ATTRIBUTE_ID_REQUIRED";
            return result;
        }

        var submittedTitle = model.CustomAttributeTitle ?? [];
        if (!submittedTitle.Any(title => !string.IsNullOrWhiteSpace(title.Label)))
        {
            result.Message = "CUSTOM_ATTRIBUTE_TITLE_REQUIRED";
            return result;
        }

        Expression<Func<CustomAttribute, bool>> whereCondition = ca => ca.CustomAttributeId == model.CustomAttributeId && ca.CompanyId == companyId;
        var existing = await _customAttributeRepository.FirstOrDefault(whereCondition);
        if (existing is null)
        {
            result.Message = "CUSTOM_ATTRIBUTE_NOT_FOUND";
            return result;
        }

        // UpdateMany reports an unchanged document as a failure. A later value-only edit
        // must remain successful when its attribute title was already saved, so only issue
        // the title update when the actual multilingual title has changed.
        var titleChanged = !existing.CustomAttributeTitle
            .OrderBy(x => x.Language, StringComparer.OrdinalIgnoreCase)
            .SequenceEqual(submittedTitle.OrderBy(x => x.Language, StringComparer.OrdinalIgnoreCase), new MultilingualTitleComparer());
        if (titleChanged)
        {
            result = await _customAttributeRepository.UpdateMany(whereCondition, Builders<CustomAttribute>.Update
                .Set(ca => ca.CustomAttributeTitle, submittedTitle)
                .Set(ca => ca.UpdatedDate, DateTime.UtcNow)
                .Set(ca => ca.UpdatedBy, userId));
            if (!result.Success) return result;
        }
        else
        {
            result.Success = true;
            result.Message = "OK";
        }

        if (model.CustomAttributeValues?.Count != 0)
        {
            // Do not rely on the generic mapper for this persistence boundary.
            // A new option must retain its submitted titles and receive a stable ID
            // before it is inserted, so the subsequent read and future edits target
            // exactly the same company-scoped document.
            foreach (CustomAttributeValueRequestDto requestedValue in model.CustomAttributeValues)
            {
                // update if custAttributeValueId is present else add new attribute item
                if (string.IsNullOrWhiteSpace(requestedValue.CustomAttributeValueId))
                {
                    CustomAttributeValue data = new()
                    {
                        CustomAttributeValueId = Guid.NewGuid().ToString(),
                        CustomAttributeId = model.CustomAttributeId,
                        CompanyId = companyId,
                        IsActive = requestedValue.IsActive,
                        Titles = requestedValue.Titles ?? [],
                        CreatedBy = userId,
                        CreatedDate = DateTime.UtcNow
                    };
                    result = await _customAttributeValueRepository.AddOne(data);
                    if (!result.Success)
                    {
                        return result;
                    }

                    // A successful write must be readable through the same tenant
                    // filter used by GetCustomAttributeById. Never report success
                    // when the option cannot be reloaded for the current company.
                    CustomAttributeValue? savedValue = await _customAttributeValueRepository.FirstOrDefault(
                        value => value.CustomAttributeValueId == data.CustomAttributeValueId
                                 && value.CustomAttributeId == model.CustomAttributeId
                                 && value.CompanyId == companyId);
                    if (savedValue is null)
                    {
                        return new Result
                        {
                            Success = false,
                            Message = "CUSTOM_ATTRIBUTE_VALUE_SAVE_FAILED"
                        };
                    }
                }
                else
                {
                    Expression<Func<CustomAttributeValue, bool>> filter = cav => cav.CustomAttributeValueId == requestedValue.CustomAttributeValueId && cav.CustomAttributeId == model.CustomAttributeId && cav.CompanyId == companyId;
                    result = await _customAttributeValueRepository.UpdateMany(filter, Builders<CustomAttributeValue>.Update.Set(v => v.UpdatedDate, DateTime.UtcNow).Set(v => v.UpdatedBy, userId).Set(v => v.Titles, requestedValue.Titles ?? []).Set(v => v.IsActive, requestedValue.IsActive));
                    if (!result.Success)
                    {
                        return result;
                    }
                }
            }
        }
        return result;
    }

    public async Task<Result> DeleteCustomAttributeValue(string customAttributeValueId, string companyId)
    {
        Result result = new();
        Expression<Func<CustomAttributeValue, bool>> expression = v => v.CustomAttributeValueId == customAttributeValueId && v.CompanyId == companyId;
        CustomAttributeValue? customAttributeValue = await _customAttributeValueRepository.FirstOrDefault(expression);
        if (customAttributeValue == null) return result;
        customAttributeValue.IsDeleted = true;
        return await _customAttributeValueRepository.Update(expression, customAttributeValue);
    }

    public async Task<Result> DeleteCustomAttribute(string customAttributeId, string companyId, string userId)
    {
        Result result = new() { Success = false };
        if (string.IsNullOrWhiteSpace(customAttributeId))
        {
            result.Message = "CUSTOM_ATTRIBUTE_ID_REQUIRED";
            return result;
        }

        Expression<Func<CustomAttribute, bool>> attributeFilter = attribute =>
            attribute.CustomAttributeId == customAttributeId && attribute.CompanyId == companyId;
        if (await _customAttributeRepository.FirstOrDefault(attributeFilter) is null)
        {
            result.Message = "CUSTOM_ATTRIBUTE_NOT_FOUND";
            return result;
        }

        var isUsedByEmployee = await _employeeRepository.Exist(employee =>
            employee.CompanyId == companyId &&
            employee.CustomAttributeList.Any(attribute => attribute.CustomAttributeId == customAttributeId));
        if (isUsedByEmployee)
        {
            result.Message = "CUSTOM_ATTRIBUTE_IN_USE";
            return result;
        }

        var values = await _customAttributeValueRepository.GetAll(value =>
            value.CompanyId == companyId && value.CustomAttributeId == customAttributeId);
        if (values.Any())
        {
            result = await _customAttributeValueRepository.UpdateMany(
                Builders<CustomAttributeValue>.Filter.Eq(value => value.CompanyId, companyId) &
                Builders<CustomAttributeValue>.Filter.Eq(value => value.CustomAttributeId, customAttributeId),
                Builders<CustomAttributeValue>.Update
                    .Set(value => value.IsDeleted, true)
                    .Set(value => value.UpdatedDate, DateTime.UtcNow)
                    .Set(value => value.UpdatedBy, userId));
            if (!result.Success) return result;
        }

        result = await _customAttributeRepository.UpdateMany(
            Builders<CustomAttribute>.Filter.Eq(attribute => attribute.CompanyId, companyId) &
            Builders<CustomAttribute>.Filter.Eq(attribute => attribute.CustomAttributeId, customAttributeId),
            Builders<CustomAttribute>.Update
                .Set(attribute => attribute.IsDeleted, true)
                .Set(attribute => attribute.UpdatedDate, DateTime.UtcNow)
                .Set(attribute => attribute.UpdatedBy, userId));
        if (result.Success) result.Message = "CUSTOM_ATTRIBUTE_DELETED";
        return result;
    }
}

