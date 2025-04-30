using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Module = Codeji.CMS.Repository.Entities.RolePermissions.Module;

namespace Codeji.CMS.Services.Employees;
public class RoleServices : IRoleService
{
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
    public async Task<string> AddEditRoles(RoleWithModuleAndPermissions roles, string companyId)
    {
        List<Roles> roleData = (await _RolesRepository.GetAll(x => x.CompanyId == companyId)).ToList();
        List<RolePermission> roleWithModulePermission = (await _rolePermissionRepository.GetAll(x => x.CompanyId == companyId)).ToList();
        if (string.IsNullOrEmpty(roles.RoleId))
        {
            Roles newRole = new Roles()
            {
                RoleType = roleData.Count + 1,
                CompanyId = companyId,
                Titles = roles.RoleTitle,
                Description = roles.Description,
                HasAppAccess = roles.HasAppAccess,
                IsNotEditable = false,
                CreatedDate = DateTime.Now

            };
            await _RolesRepository.AddOne(newRole);
            foreach (ModuleRolePermissionsModel permission in roles.RolePermissions)
            {
                await _rolePermissionRepository.AddOne(new RolePermission
                {
                    RolePermissionId = permission.RolePermissionId,
                    CompanyId = companyId,
                    ModulePermissionId = permission.ModulePermissionId,
                    RoleId = newRole.RolesId,
                    HasAccess = permission.HasAccess
                });
            }
            return "ROLE.ADD.SUCCESS";
        }
        else
        {

            Task<Roles?> currentRole = _RolesRepository.FirstOrDefault(x => x.RolesId == roles.RoleId);
            if (currentRole != null)
            {
                Expression<Func<Roles, bool>> roleWhereCondition = x => x.CompanyId == companyId && x.RolesId == roles.RoleId;
                await _RolesRepository.UpdateMany(roleWhereCondition, Builders<Roles>.Update.Set(x => x.Titles, roles.RoleTitle)
                    .Set(x => x.Description, roles.Description)
                    .Set(x => x.UpdatedDate, DateTime.UtcNow));

                foreach (ModuleRolePermissionsModel currentRolePermission in roles.RolePermissions)
                {
                    Task<RolePermission?> data = _rolePermissionRepository.FirstOrDefault(x => x.RoleId == roles.RoleId && x.RolePermissionId == currentRolePermission.RolePermissionId);
                    if (data.Result != null)
                    {
                        Expression<Func<RolePermission, bool>> whereCondition = x => x.RoleId == currentRolePermission.RolesId && x.RolePermissionId == currentRolePermission.RolePermissionId;
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
                            IsAccessible = false,
                            CompanyId = companyId

                        };
                        await _rolePermissionRepository.AddOne(newPermission);
                    }
                }
            }
            return "ROLE.UPDATE.SUCCESS";
        }
    }
    //Get company's all roles
    public async Task<List<RoleModel>> GetRoles(string companyId)
    {
        List<Roles> roles = (await _RolesRepository.GetAll(x => x.CompanyId == companyId)).ToList();
        return _mapper.Map<List<RoleModel>>(roles);
    }
    //Fetch matched role with given Id
    public async Task<RoleModel> GetRoleById(string roleId)
    {
        Roles role = await _RolesRepository.FirstOrDefault(x => x.RolesId == roleId);
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
            IEnumerable<RolePermission> rolePermissions = await _rolePermissionRepository.GetAll(x => x.RoleId == role.RolesId);
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
    public async Task<string[]> GetRolePermissionOfuser(string roleId)
    {
        List<ModuleWithPermissionsModel> moduleWithPermissionsModel = new();
        Expression<Func<Roles, bool>> whereCondition = x => x.RolesId == roleId;
        IEnumerable<Permission> permissions = await _permissionRepository.GetAll();
        IEnumerable<Module> modules = await _moduleRepository.GetAll();
        IEnumerable<ModulePermission> modulePermissions = await _modulePermissionRepository.GetAll();

        Roles? role = await _RolesRepository.FirstOrDefault(whereCondition);
        string[] res = Array.Empty<string>();
        if (role != null)
        {
            List<int> rolePermissions = _rolePermissionRepository.Get(x => x.RoleId == role.RolesId && x.IsAccessible && x.HasAccess).Select(x => x.ModulePermissionId).ToList();


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
        List<Roles> roles = _RolesRepository.Get(x => x.IsDefault && string.IsNullOrEmpty(x.CompanyId)).ToList();
        List<string> roleIds = roles.Select(x => x.RolesId).ToList();
        IEnumerable<RolePermission> rolePermissions = await _rolePermissionRepository.GetAll(x => roleIds.Contains(x.RoleId));
        foreach (Roles? role in roles)
        {
            string oldRoleId = role.RolesId;
            role.RolesId = Guid.NewGuid().ToString();
            role.CompanyId = companyId;
            role.IsDefault = false;
            role.CreatedDate = DateTime.Now;
            if (role.RoleType == 1)
                role.HasAppAccess = true;
            List<RolePermission> permissions = rolePermissions.Where(x => x.RoleId == oldRoleId).ToList();
            foreach (RolePermission? item in permissions)
            {
                Task<Domain.Models.Result> rolePermission = _rolePermissionRepository.AddOne(
                    new RolePermission
                    {
                        ModulePermissionId = item.ModulePermissionId,
                        RoleId = role.RolesId,
                        CreatedDate = DateTime.Now,
                        CompanyId = companyId,
                        IsAccessible = item.IsAccessible,
                        HasAccess = item.HasAccess
                    });
                //await _rolePermissionRepository.AddOne(item);
            }
        }
        await _RolesRepository.AddMany(roles);
        return roles;
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
        Expression<Func<EmpUser, bool>> whereUserCondtion = x => roleIds.Contains(x.RoleId);
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
            UserModel user = _middleware.GetUserById(userId);
            string[] modulePremissions = await GetRolePermissionOfuser(user.RoleId);
            string[] permission = Role.Select(_ => $"{module}.{_}").ToArray();
            if (!string.IsNullOrEmpty(module))
                modules = new string[] { module };
            hasPermission = permission.Any(x => modulePremissions.Contains(x));

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
                StatusCode = 200,
                Message = "ROLE.NOT_EXIST",
                Success = false
            };
        }
        Expression<Func<Roles, bool>> whereCondition = x => x.RolesId == roleId;
        await _RolesRepository.UpdateMany(whereCondition, Builders<Roles>.Update
            .Set(x => x.HasAppAccess, hasAppAccess)
            .Set(x => x.UpdatedDate, DateTime.UtcNow));

        return new Result(){
            StatusCode = 200,
            Message = "ROLE.APP_ACCESS.UPDATED",
            Success = true
        };
    }
    public async Task<List<AllModuleDetailsResponseModel>> GetAllModulesDetails(string companyId)
    {
        //IEnumerable<Module> allmodules = await _moduleRepository.GetAll() ?? null ;


        var allModules = await _moduleRepository.GetAll(x => x.ModuleName != "Company Details"); 
        // Not selecting "Company Details" as it will hide Module Accessibility Controls to Admin.

        var allModulePermissions = await _modulePermissionRepository.GetAll();
        var allRolePermission = await _rolePermissionRepository.GetAll(x=>x.CompanyId == companyId);

        var queryResult = from module in allModules
                          join modulePermission in allModulePermissions on module.ModuleId equals modulePermission.ModuleId into modulePermissionGroup
                          from modulePermission in modulePermissionGroup.Take(1)
                          join rolePermission in allRolePermission on modulePermission.ModulePermissionId equals rolePermission.ModulePermissionId into roleGroup
                          from rolePermission in roleGroup.Take(1)
                          select new AllModuleDetailsResponseModel
                          {
                              ModuleId = module.ModuleId,
                              ModuleName = module.ModuleName,
                              ModuleConstant = module.ModuleConstant,
                              IsAccessible = rolePermission.IsAccessible,
                          };

        return queryResult.ToList();

        //return role.ToList();
        //return _mapper.Map<List<AllModuleDetailsResponseModel>>(allmodules);
    }
}