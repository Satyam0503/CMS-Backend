using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using MapsterMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Enums;
using AppModule = Codeji.CMS.Utility.Constraints.AppModule;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Module = Codeji.CMS.Repository.Entities.RolePermissions.Module;
using PermissionConst = Codeji.CMS.Utility.Constraints.Permission;

namespace Codeji.CMS.Services.Employees;

public class RoleServices : IRoleService
{
    private static readonly (int RoleType, string Title, string Description, bool IsNotEditable)[] DefaultRoleTemplates =
    [
        ((int)EnumsHelper.Roles.Administrator, "Company Administrator", "Administrators have all permissions in the app. Limit this role to employees who will be in charge the client account", true),
        ((int)EnumsHelper.Roles.HR, "HR Manager", "The HR employee can add and edit colleagues, manage attendance, leave, and assigned roles.", false),
        ((int)EnumsHelper.Roles.HRExecutive, "HR Executive", "HR Executives have the same application permissions as HR Managers.", false),
        ((int)EnumsHelper.Roles.Employee, "Employee", "Regular Employee", false),
    ];

    private readonly IMongoDbRepository<Roles> _RolesRepository;
    private readonly IMongoDbRepository<RolePermission> _rolePermissionRepository;
    private readonly IMongoDbRepository<ModulePermission> _modulePermissionRepository;
    private readonly IMongoDbRepository<Module> _moduleRepository;
    private readonly IMongoDbRepository<Permission> _permissionRepository;
    private readonly IMongoDbRepository<EmpUser> _userRepository;
    private readonly IMapper _mapper;
    private readonly IMiddlewareService _middleware;

    public RoleServices(
        IMongoDbRepository<Roles> RolesRepository,
        IMongoDbRepository<RolePermission> rolePermissionRepository,
        IMongoDbRepository<ModulePermission> modulePermissionRepository,
        IMongoDbRepository<Module> moduleRepository,
        IMongoDbRepository<Permission> permissionRepository,
        IMongoDbRepository<EmpUser> userRepository,
        IMapper mapper,
        IMiddlewareService middleware)
    {
        _RolesRepository = RolesRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _modulePermissionRepository = modulePermissionRepository;
        _moduleRepository = moduleRepository;
        _permissionRepository = permissionRepository;
        _userRepository = userRepository;
        _mapper = mapper;
        _middleware = middleware;
    }

    //Add New Roles
    public async Task<Result> AddEditRoles(RoleWithModuleAndPermissions roles, string companyId)
    {
        Result result = new();
        int totalRoles = await _RolesRepository.Count(x => x.CompanyId == companyId);
        List<RolePermission> roleWithModulePermission = (await _rolePermissionRepository.GetAll(x => x.CompanyId == companyId)).ToList();
        if (string.IsNullOrEmpty(roles.RoleId))
        {
            Roles newRole = new Roles()
            {
                RoleType = totalRoles + 1,
                CompanyId = companyId,
                Titles = roles.RoleTitle,
                Description = roles.Description,
                HasAppAccess = roles.HasAppAccess,
                IsNotEditable = false,
                CreatedDate = DateTime.UtcNow
            };
            await _RolesRepository.AddOne(newRole);
            List<RolePermission> rolePermissions = [];
            foreach (ModuleRolePermissionsModel permission in roles.RolePermissions)
            {
                rolePermissions.Add(new RolePermission
                {
                    RolePermissionId = permission.RolePermissionId,
                    CompanyId = companyId,
                    ModulePermissionId = permission.ModulePermissionId,
                    RoleId = newRole.RolesId,
                    HasAccess = permission.HasAccess
                });
            }
            result = await _rolePermissionRepository.AddMany(rolePermissions);
            return result;
        }
        else
        {

            Roles? currentRole = await _RolesRepository.FirstOrDefault(x => x.CompanyId == companyId && x.RolesId == roles.RoleId && !x.IsDeleted);
            if (currentRole != null)
            {
                Expression<Func<Roles, bool>> roleWhereCondition = x => x.CompanyId == companyId && x.RolesId == roles.RoleId;
                await _RolesRepository.UpdateMany(roleWhereCondition, Builders<Roles>.Update.Set(x => x.Titles, roles.RoleTitle)
                    .Set(x => x.Description, roles.Description)
                    .Set(x => x.UpdatedDate, DateTime.UtcNow));

                foreach (ModuleRolePermissionsModel currentRolePermission in roles.RolePermissions)
                {
                    RolePermission? data = await _rolePermissionRepository.FirstOrDefault(x => x.CompanyId == companyId && x.RoleId == roles.RoleId && x.ModulePermissionId == currentRolePermission.ModulePermissionId);
                    if (data != null)
                    {
                        Expression<Func<RolePermission, bool>> whereCondition = x => x.CompanyId == companyId && x.RoleId == roles.RoleId && x.ModulePermissionId == currentRolePermission.ModulePermissionId;
                        await _rolePermissionRepository.UpdateMany(whereCondition, Builders<RolePermission>.Update
                            .Set(x => x.HasAccess, currentRolePermission.HasAccess)
                            .Set(x => x.UpdatedDate, DateTime.UtcNow));
                    }
                    else
                    {
                        RolePermission newPermission = new RolePermission()
                        {
                            RoleId = roles.RoleId,
                            ModulePermissionId = currentRolePermission.ModulePermissionId,
                            HasAccess = currentRolePermission.HasAccess,
                            IsAccessible = true,
                            CompanyId = companyId
                        };
                        await _rolePermissionRepository.AddOne(newPermission);
                    }
                }
            }
            result.Success = true;
            return result;
        }
    }
    //Get company's all roles
    public async Task<List<RoleModel>> GetRoles(string companyId, bool? excludeAdmin)
    {
        Expression<Func<Roles, bool>> expression = r => r.CompanyId == companyId;
        if (excludeAdmin.HasValue && excludeAdmin.Value)
        {
            expression = r => r.CompanyId == companyId && r.RoleType != Convert.ToInt32(EnumsHelper.Roles.Administrator) && r.Titles != "Company Administrator";
        }
        // Hide historical duplicate role rows while the data migration removes
        // them. RoleType is the tenant's stable default-role identity.
        IEnumerable<Roles> roles = (await _RolesRepository.GetAll(expression))
            .GroupBy(r => r.RoleType)
            .Select(group => group.OrderByDescending(r => r.UpdatedDate).ThenByDescending(r => r.CreatedDate).First())
            .OrderBy(r => r.RoleType)
            .ToList();
        foreach (var role in roles)
        {
            if (!await _rolePermissionRepository.Exist(permission =>
                    permission.CompanyId == companyId && permission.RoleId == role.RolesId))
            {
                await RestoreCompanyRolePermissionsAsync(role);
            }
        }
        return _mapper.Map<List<RoleModel>>(roles);
    }
    //Fetch matched role with given Id
    public async Task<RoleModel> GetRoleById(string roleId, string companyId)
    {
        Roles role = await _RolesRepository.FirstOrDefault(x =>
            x.RolesId == roleId && x.CompanyId == companyId && !x.IsDeleted);
        return _mapper.Map<RoleModel>(role);
    }
    //Saving role and it's permission
    //public async Task<RoleWithModuleAndPermissions> SaveRoleAndPermissions(RoleWithModuleAndPermissions roleWithModuleAndPermissions)
    //{
    //    if (roleWithModuleAndPermissions == null)
    //    {
    //        return null;
    //    }

    //    Sanitizer.SanitizeProperties(roleWithModuleAndPermissions);
    //    Roles Roles = await AddEditRoles(new Roles
    //    {
    //        Titles = roleWithModuleAndPermissions.RoleTitle,
    //        RolesId = roleWithModuleAndPermissions.RoleId,
    //        Description = roleWithModuleAndPermissions.Description,
    //        UserRoles = roleWithModuleAndPermissions.UserRoles,
    //        HasAppAccess = roleWithModuleAndPermissions.HasAppAccess,
    //        UpdatedDate = DateTime.Now
    //    });
    //    foreach (ModuleRolePermissionsModel permission in roleWithModuleAndPermissions.RolePermissions)
    //    {
    //        permission.RolesId = Roles.RolesId;
    //        await saveRolePermission(new RolePermission
    //        {
    //            RolePermissionId = permission.RolePermissionId,
    //            ModulePermissionId = permission.ModulePermissionId,
    //            RoleId = permission.RolesId,
    //            HasAccess = permission.HasAccess
    //        });
    //    }
    //    roleWithModuleAndPermissions.RoleId = Roles.RolesId;
    //    return roleWithModuleAndPermissions;
    //}

    //Get all roles with their permission
    public async Task<List<ModuleWithPermissionsModel>> GetRoleWithPermissions(string roleId, string companyId)
    {
        List<ModuleWithPermissionsModel> moduleWithPermissionsModel = new();
        Expression<Func<Roles, bool>> whereCondition = x => x.CompanyId == companyId && x.RolesId == roleId;
        Roles role = await _RolesRepository.FirstOrDefault(whereCondition);
        if (role != null)
        {
            IEnumerable<RolePermission> rolePermissions = await _rolePermissionRepository.GetAll(x => x.CompanyId == companyId && x.RoleId == role.RolesId);
            if (!rolePermissions.Any())
            {
                await RestoreCompanyRolePermissionsAsync(role);
                rolePermissions = await _rolePermissionRepository.GetAll(x => x.CompanyId == companyId && x.RoleId == role.RolesId);
            }
            List<ModulePermission> modulePermissions = (await _modulePermissionRepository.GetAll()).ToList();
            List<Permission> permissions = (await _permissionRepository.GetAll()).ToList();
            List<Module> modules = (await _moduleRepository.GetAll()).ToList();
            var result = (from rp in rolePermissions
                          join mp in modulePermissions on rp.ModulePermissionId equals mp.ModulePermissionId
                          join m in modules on mp.ModuleId equals m.ModuleId
                          join p in permissions on mp.PermissionId equals p.PermissionId
                          where rp.RoleId == role.RolesId
                          select new { permission = p, rolePermissions = rp, module = m, modulePermission = mp }).ToList();
            var Roless = result.Select(x => new { x.module.ModuleId, x.module.ModuleName, x.module.ModuleConstant }).Distinct().ToArray();
            for (int i = 0; i < Roless.Length; i++)
            {
                var Roles = Roless[i];
                ModuleWithPermissionsModel moduleWithPermissionModel = new()
                {
                    ModuleName = Roles.ModuleName,
                    ModuleConstant = Roles.ModuleConstant
                };
                List<ModuleRolePermissionsModel> permissionsList = result.Where(x => x.module.ModuleId == Roles.ModuleId).Select(y => new ModuleRolePermissionsModel
                {
                    PermissionName = y.permission.PermissionName,
                    RolePermissionId = y.rolePermissions.RolePermissionId,
                    ModulePermissionId = y.modulePermission.ModulePermissionId,
                    RolesId = y.rolePermissions.RoleId,
                    HasAccess = y.rolePermissions.HasAccess,
                    PermissionConstant = y.permission.PermissionConstant
                }).ToList();
                moduleWithPermissionModel.Permissions = permissionsList;
                moduleWithPermissionsModel.Add(moduleWithPermissionModel);
            }
        }
        return moduleWithPermissionsModel;
    }

    private async Task RestoreCompanyRolePermissionsAsync(Roles companyRole)
    {
        var sourceRole = (await _RolesRepository.GetAll(
                role => role.IsDefault && string.IsNullOrEmpty(role.CompanyId) &&
                        !role.IsDeleted && role.RoleType == companyRole.RoleType,
                withDefaultFilter: false))
            .OrderByDescending(role => role.UpdatedDate)
            .ThenByDescending(role => role.CreatedDate)
            .FirstOrDefault();
        if (sourceRole is null)
            return;

        var templatePermissions = await _rolePermissionRepository.GetAll(
            permission => permission.RoleId == sourceRole.RolesId,
            withDefaultFilter: false);
        var restoredPermissions = templatePermissions
            .GroupBy(permission => permission.ModulePermissionId)
            .Select(group => group.First())
            .Select(permission => new RolePermission
            {
                RoleId = companyRole.RolesId,
                CompanyId = companyRole.CompanyId,
                ModulePermissionId = permission.ModulePermissionId,
                HasAccess = permission.HasAccess,
                IsAccessible = permission.IsAccessible,
                CreatedDate = DateTime.UtcNow,
            })
            .ToList();
        if (restoredPermissions.Count > 0)
            await _rolePermissionRepository.AddMany(restoredPermissions);
    }
    public async Task<string[]> GetRolePermissionOfuser(string roleId, string companyId)
    {
        if (string.IsNullOrWhiteSpace(roleId) || string.IsNullOrWhiteSpace(companyId)) return Array.Empty<string>();
        Expression<Func<Roles, bool>> whereCondition = x => x.RolesId == roleId && x.CompanyId == companyId && !x.IsDeleted;
        IEnumerable<Permission> permissions = await _permissionRepository.GetAll();
        IEnumerable<Module> modules = await _moduleRepository.GetAll();
        IEnumerable<ModulePermission> modulePermissions = await _modulePermissionRepository.GetAll();

        Roles? role = await _RolesRepository.FirstOrDefault(whereCondition);
        string[] res = Array.Empty<string>();
        if (role != null)
        {
            List<int> rolePermissions = _rolePermissionRepository.Get(x => x.CompanyId == companyId && x.RoleId == role.RolesId && x.IsAccessible && x.HasAccess).Select(x => x.ModulePermissionId).ToList();


            res = (from rp in rolePermissions
                    join mp in modulePermissions on rp equals mp.ModulePermissionId
                    join m in modules on mp.ModuleId equals m.ModuleId
                    join p in permissions on mp.PermissionId equals p.PermissionId
                    select String.Format($"{m.ModuleConstant}.{p.PermissionConstant}")).OrderBy(x => x).ToArray();
        }
        return res;
    }
    public async Task<List<Roles>> AddDefaultRole(string companyId)
    {
        // Older databases can contain more than one global template for a role type.
        // A company must still receive exactly one Administrator, HR Manager, and
        // Employee role, and deleted templates must never be copied.
        List<Roles> roles = _RolesRepository.Get(x => x.IsDefault &&
                                                   string.IsNullOrEmpty(x.CompanyId) &&
                                                   !x.IsDeleted)
            .GroupBy(x => x.RoleType)
            .Select(group => group
                .OrderByDescending(x => x.UpdatedDate)
                .ThenByDescending(x => x.CreatedDate)
                .First())
            .ToList();
        roles = await EnsureDefaultRoleTemplatesAsync(roles);
        var existingCompanyRoles = (await _RolesRepository.GetAll(x => x.CompanyId == companyId && !x.IsDeleted))
            .GroupBy(x => x.RoleType)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(x => x.UpdatedDate).ThenByDescending(x => x.CreatedDate).First());
        List<Roles> rolesToCreate = roles.Where(role => !existingCompanyRoles.ContainsKey(role.RoleType)).ToList();
        if (rolesToCreate.Count == 0)
            return existingCompanyRoles.Values.OrderBy(x => x.RoleType).ToList();

        List<string> roleIds = rolesToCreate.Select(x => x.RolesId).ToList();
        // Global role templates are intentionally outside a tenant. Registration
        // has no company context yet, so this is the explicit scoped exception
        // needed to copy their canonical grants into the new company.
        IEnumerable<RolePermission> rolePermissions = await _rolePermissionRepository.GetAll(
            x => roleIds.Contains(x.RoleId),
            withDefaultFilter: false);
        var companyRolePermissions = new List<RolePermission>();
        foreach (Roles role in rolesToCreate)
        {
            string oldRoleId = role.RolesId;
            role.RolesId = Guid.NewGuid().ToString();
            role.CompanyId = companyId;
            role.IsDefault = false;
            role.CreatedDate = DateTime.Now;
            if (role.RoleType == 1)
                role.HasAppAccess = true;
            // A duplicate template may also contain duplicate permissions.  Keep
            // one row per module permission for the new company role.
            foreach (RolePermission item in rolePermissions
                         .Where(x => x.RoleId == oldRoleId)
                         .GroupBy(x => x.ModulePermissionId)
                         .Select(group => group.First()))
            {
                companyRolePermissions.Add(new RolePermission
                {
                    ModulePermissionId = item.ModulePermissionId,
                    RoleId = role.RolesId,
                    CreatedDate = DateTime.UtcNow,
                    CompanyId = companyId,
                    IsAccessible = item.IsAccessible,
                    HasAccess = item.HasAccess
                });
            }
        }
        await _RolesRepository.AddMany(rolesToCreate);
        if (companyRolePermissions.Count > 0)
            await _rolePermissionRepository.AddMany(companyRolePermissions);
        return existingCompanyRoles.Values.Concat(rolesToCreate).OrderBy(x => x.RoleType).ToList();
    }

    // Registration must not depend on an operational migration having already
    // populated the global role templates. When they are missing, restore the
    // same canonical templates and grants that the seed migration creates.
    private async Task<List<Roles>> EnsureDefaultRoleTemplatesAsync(List<Roles> templates)
    {
        var missingTemplates = DefaultRoleTemplates
            .Where(definition => templates.All(role => role.RoleType != definition.RoleType))
            .Select(definition => new Roles
            {
                RolesId = Guid.NewGuid().ToString(),
                RoleType = definition.RoleType,
                CompanyId = null!,
                Titles = definition.Title,
                Description = definition.Description,
                HasAppAccess = true,
                IsNotEditable = definition.IsNotEditable,
                IsDefault = true,
                IsDeleted = false,
                UserRoles = [],
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                CreatedBy = string.Empty,
                UpdatedBy = string.Empty,
            })
            .ToList();

        if (missingTemplates.Count == 0)
            return templates;

        await _RolesRepository.AddMany(missingTemplates);
        await AddDefaultTemplatePermissionsAsync(missingTemplates);
        return templates.Concat(missingTemplates).OrderBy(role => role.RoleType).ToList();
    }

    private async Task AddDefaultTemplatePermissionsAsync(IEnumerable<Roles> templates)
    {
        var modulePermissions = (await _modulePermissionRepository.GetAll()).ToList();
        if (modulePermissions.Count == 0)
            return;

        var modules = (await _moduleRepository.GetAll()).ToDictionary(module => module.ModuleId, module => module.ModuleConstant);
        var permissions = (await _permissionRepository.GetAll()).ToDictionary(permission => permission.PermissionId, permission => permission.PermissionConstant);
        var modulePermissionIds = modulePermissions
            .Where(modulePermission => modules.ContainsKey(modulePermission.ModuleId) && permissions.ContainsKey(modulePermission.PermissionId))
            .ToDictionary(
                modulePermission => (modules[modulePermission.ModuleId], permissions[modulePermission.PermissionId]),
                modulePermission => modulePermission.ModulePermissionId);

        var grants = new List<RolePermission>();
        foreach (var template in templates)
        {
            IEnumerable<int> permissionIds = template.RoleType switch
            {
                (int)EnumsHelper.Roles.Administrator => modulePermissions.Select(permission => permission.ModulePermissionId),
                (int)EnumsHelper.Roles.HR or (int)EnumsHelper.Roles.HRExecutive => ResolveModulePermissionIds(modulePermissionIds, HrManagerPermissions()),
                (int)EnumsHelper.Roles.Employee => ResolveModulePermissionIds(modulePermissionIds, EmployeePermissions()),
                _ => [],
            };

            grants.AddRange(permissionIds.Distinct().Select(modulePermissionId => new RolePermission
            {
                RoleId = template.RolesId,
                ModulePermissionId = modulePermissionId,
                HasAccess = true,
                IsAccessible = true,
                CreatedDate = DateTime.UtcNow,
            }));
        }

        if (grants.Count > 0)
            await _rolePermissionRepository.AddMany(grants);
    }

    private static IEnumerable<int> ResolveModulePermissionIds(
        IReadOnlyDictionary<(string Module, string Permission), int> modulePermissionIds,
        IEnumerable<(string Module, string Permission)> requiredPermissions) =>
        requiredPermissions
            .Where(required => modulePermissionIds.ContainsKey(required))
            .Select(required => modulePermissionIds[required]);

    private static IEnumerable<(string Module, string Permission)> FullAccess(string module)
    {
        yield return (module, PermissionConst.View);
        yield return (module, PermissionConst.Create);
        yield return (module, PermissionConst.Edit);
        yield return (module, PermissionConst.Delete);
    }

    private static IEnumerable<(string Module, string Permission)> HrManagerPermissions()
    {
        foreach (var permission in FullAccess(AppModule.Employees)) yield return permission;
        foreach (var permission in FullAccess(AppModule.Attendance)) yield return permission;
        foreach (var permission in FullAccess(AppModule.LeaveManagement)) yield return permission;
        foreach (var permission in FullAccess(AppModule.Calendar)) yield return permission;
        foreach (var permission in FullAccess(AppModule.NoticeBoard)) yield return permission;
        foreach (var permission in FullAccess(AppModule.Jobs)) yield return permission;
        foreach (var permission in FullAccess(AppModule.Applications)) yield return permission;
        yield return (AppModule.ProcessLog, PermissionConst.View);
        yield return (AppModule.PayRoll, PermissionConst.Create);
        yield return (AppModule.PayRoll, PermissionConst.Edit);
        yield return (AppModule.PayRoll, PermissionConst.Delete);
        foreach (var permission in FullAccess(AppModule.PayrollSettings)) yield return permission;
        yield return (AppModule.Policy, PermissionConst.View);
    }

    private static IEnumerable<(string Module, string Permission)> EmployeePermissions()
    {
        yield return (AppModule.Employees, PermissionConst.Create);
        foreach (var permission in FullAccess(AppModule.Calendar)) yield return permission;
        yield return (AppModule.NoticeBoard, PermissionConst.Create);
        yield return (AppModule.PayRoll, PermissionConst.Edit);
        yield return (AppModule.PayrollSettings, PermissionConst.Edit);
    }

    //Get default roles with their permission
    public async Task<List<ModuleWithPermissionsModel>> GetDefaultRoleWithPermissions(bool isEditableUserRole, string companyId)
    {
        List<ModuleWithPermissionsModel> moduleWithPermissionsModel = new();
        Expression<Func<Roles, bool>> whereCondition = x => x.CompanyId == companyId && x.IsNotEditable == true;
        Roles role = await _RolesRepository.FirstOrDefault(whereCondition);
        if (role != null)
        {
            moduleWithPermissionsModel = await GetRoleWithPermissions(role.RolesId, role.CompanyId);
        }
        return moduleWithPermissionsModel;
    }
    //Get company's all roles
    public async Task<List<RoleModel>> GetRolesWithPagination(int pageNo, int pageSize)
    {
        int skiprecords = (pageNo - 1) * pageSize;
        Expression<Func<Roles, bool>> whereCondition = x => true;
        List<Roles> roles = (await _RolesRepository.GetAll()).ToList();
        return await returnRolesList(roles);
    }
    //Delete role if no user exist with it
    public async Task<bool> CheckRoleDependancyForDeletion(string roleId)
    {
        bool result = false;
        int count = await _userRepository.Count(x => x.RoleId == roleId);
        if (count > 0)
        {
            result = true;
        }
        else
        {
            await DeleteRole(roleId);
        }
        return result;
    }
    private async Task<bool> DeleteRole(string roleId)
    {
        bool result = false;
        Expression<Func<Roles, bool>> whereCondition = x => x.RolesId == roleId && !x.IsNotEditable;
        Roles Role = await _RolesRepository.FirstOrDefault(whereCondition);
        if (Role != null)
        {
            Role.IsDeleted = true;
            await _RolesRepository.Update(whereCondition, Role);
            result = true;
        }
        return result;
    }
    public async Task<List<string>> GetUsersByRole(string[] roleIds, string companyId)
    {
        Expression<Func<EmpUser, bool>> whereUserCondtion = x => roleIds.Contains(x.RoleId) && x.CompanyId == companyId;
        List<string> users = await _userRepository.Get(whereUserCondtion).Select(x => x.UserId).ToListAsync();
        return users;
    }

    #region Private Methods
    //Transform Company role with permission
    private async Task<List<RoleModel>> returnRolesList(List<Roles> roles)
    {
        // for roles connection to the application permission change
        string[] rolesIds = roles.Select(x => x.RolesId).ToArray();
        // 27 for Dashboard acces permission.
        IEnumerable<RolePermission> rolePermissions = await _rolePermissionRepository.GetAll(x => rolesIds.Contains(x.RoleId));
        return (from role in roles
                join rp in rolePermissions on role.RolesId equals rp.RoleId
                select new RoleModel
                {
                    RolesId = role.RolesId,
                    Description = role.Description,
                    Titles = role.Titles,
                    UserRoles = role.UserRoles ?? new(),
                    IsDefault = role.IsDefault,
                    IsNotEditable = role.IsNotEditable
                }).ToList();
    }
    //Save role permission
    private async Task<RolePermission> saveRolePermission(RolePermission permission)
    {
        if (permission == null)
        {
            return null;
        }

        RolePermission rolePermission = await _rolePermissionRepository.FirstOrDefault(x => x.RoleId == permission.RoleId && x.RolePermissionId == permission.RolePermissionId);
        if (rolePermission != null)
        {
            FilterDefinition<RolePermission> filter = Builders<RolePermission>.Filter.Where(e => e.RoleId == permission.RoleId && e.RolePermissionId == permission.RolePermissionId);
            rolePermission.HasAccess = permission.HasAccess;
            rolePermission.UpdatedBy = "saveRolePermission";
            rolePermission.UpdatedDate = DateTime.UtcNow;
            await _rolePermissionRepository.Update(filter, rolePermission);
        }
        else
        {
            rolePermission = new RolePermission()
            {
                HasAccess = permission.HasAccess,
                RoleId = permission.RoleId,
                ModulePermissionId = permission.ModulePermissionId,
                CreatedBy = "saveRolePermission",
                CreatedDate = DateTime.UtcNow,
            };
            await _rolePermissionRepository.AddOne(rolePermission);
        }
        return rolePermission;
    }

    //Verify Login User with role permissions
    #endregion
    public async Task<bool> VerifyUserAccess(string module, string[] Role, string userId, string companyId, UserCheckModel userForEdit = null)
    {
        bool hasPermission = false;
        string[] modules = Array.Empty<string>();
        // if (userForEdit != null && !string.IsNullOrEmpty(userForEdit.UserId) && Role.Contains("Constants.Colleagues_Edit") && userId == userForEdit.UserId)
        // {
        //     UserModel users = _middleware.GetUserById(userForEdit.UserId);
        //     if (users != null && users.RoleId == userForEdit.RoleId)
        //         return true;
        // }

        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(companyId))

        {
            UserModel? user = await _middleware.GetUserById(userId);
            if (user is null || user.CompanyId != companyId)
            {
                return hasPermission;
            }

            var role = await _RolesRepository.FirstOrDefault(x => x.RolesId == user.RoleId && x.CompanyId == companyId);
            if (role?.RoleType == (int)EnumsHelper.Roles.Administrator)
            {
                return true;
            }

            string[] modulePermissions = await GetRolePermissionOfuser(user.RoleId, companyId);
            string[] permission = Role.Select(_ => $"{module}.{_}").ToArray();
            if (!string.IsNullOrEmpty(module))
                modules = new string[] { module };
            hasPermission = permission.Any(x => modulePermissions.Contains(x));

            // HR system roles use their company's dashboard. Dashboard access is
            // role-owned (the React dashboard follows the same rule), while the
            // attendance compatibility path below still requires an assigned
            // attendance module permission.
            bool isHrRole = role?.RoleType == (int)EnumsHelper.Roles.HR || role?.RoleType == (int)EnumsHelper.Roles.HRExecutive;
            bool hasAnyModulePermission = modulePermissions.Any(x => x.StartsWith($"{module}.", StringComparison.Ordinal));
            if (!hasPermission && isHrRole &&
                (module == AppModule.Dashboard || (module == AppModule.Attendance && hasAnyModulePermission)))
            {
                hasPermission = true;
            }

        }

        return hasPermission;
    }


    //Get All Roles
    public async Task<List<ModuleWithPermissionsModel>> GetAllRolesWithPermission(string companyId)
    {
        List<ModuleWithPermissionsModel> moduleWithPermissionsModel = new();
        Expression<Func<Roles, bool>> whereCondition = x => x.CompanyId == companyId;
        List<ModulePermission> modulePermissions = (await _modulePermissionRepository.GetAll()).ToList();
        List<Permission> permissions = (await _permissionRepository.GetAll()).ToList();
        List<Module> modules = (await _moduleRepository.GetAll()).ToList();
        var result = (from mp in modulePermissions
                      join m in modules on mp.ModuleId equals m.ModuleId
                      join p in permissions on mp.PermissionId equals p.PermissionId
                      select new { permission = p, module = m, modulePermission = mp }).ToList();
        var Roless = result.Select(x => new { x.module.ModuleId, x.module.ModuleName, x.module.ModuleConstant }).Distinct().ToArray();
        for (int i = 0; i < Roless.Length; i++)
        {
            var Roles = Roless[i];
            ModuleWithPermissionsModel moduleWithPermissionModel = new()
            {
                ModuleName = Roles.ModuleName,
                ModuleConstant = Roles.ModuleConstant
            };
            List<ModuleRolePermissionsModel> permissionsList = result.Where(x => x.module.ModuleId == Roles.ModuleId).Select(y => new ModuleRolePermissionsModel
            {
                PermissionName = y.permission.PermissionName,
                ModulePermissionId = y.modulePermission.ModulePermissionId,
                PermissionConstant = y.permission.PermissionConstant
            }).ToList();
            moduleWithPermissionModel.Permissions = permissionsList;
            moduleWithPermissionsModel.Add(moduleWithPermissionModel);
        }
        return moduleWithPermissionsModel;
    }

    public async Task<Result> UpdateAppAccessForRole(string roleId, bool hasAppAccess)
    {
        var isRoleExist = await _RolesRepository.Exist(x => x.RolesId == roleId);
        if (!isRoleExist)
        {
            return new Result()
            {
                Success = false
            };
        }
        Expression<Func<Roles, bool>> whereCondition = x => x.RolesId == roleId;
        await _RolesRepository.UpdateMany(whereCondition, Builders<Roles>.Update
            .Set(x => x.HasAppAccess, hasAppAccess)
            .Set(x => x.UpdatedDate, DateTime.UtcNow));

        return new Result()
        {
            Success = true
        };
    }

    // Check roleId is admin roleId 

    public async Task<bool> IsRoleTypeMatch(string roleId, EnumsHelper.Roles roleType, string companyId)
    {
        if (string.IsNullOrEmpty(roleId))
        {
            return false;
        }
        Expression<Func<Roles, bool>> expression = r => r.RolesId == roleId && r.CompanyId == companyId && r.RoleType == (int)roleType;
        Roles? roles = await _RolesRepository.FirstOrDefault(expression);
        if (roles == null) return false;
        return true;
    }

}
