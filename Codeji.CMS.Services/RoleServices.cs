using System.Data;
using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Module = Codeji.CMS.Repository.Entities.RolePermissions.Module;

namespace Codeji.CMS.Services;
public class RoleServices : IRoleService
{
    private readonly IMongoDbRepository<Roles> _RolesRepository;
    private readonly IMongoDbRepository<RolePermission> _rolePermissionRepository;
    private readonly IMongoDbRepository<ModulePermission> _modulePermissionRepository;
    private readonly IMongoDbRepository<Module> _moduleRepository;
    private readonly IMongoDbRepository<Permission> _permissionRepository;
    private readonly IMongoDbRepository<User> _userRepository;
    private readonly IMapper _mapper;
    private readonly IMiddlewareService _middleware;

    public RoleServices(
        IMongoDbRepository<Roles> RolesRepository,
        IMongoDbRepository<RolePermission> rolePermissionRepository,
        IMongoDbRepository<ModulePermission> modulePermissionRepository,
        IMongoDbRepository<Module> moduleRepository,
        IMongoDbRepository<Permission> permissionRepository,
        IMongoDbRepository<User> userRepository,
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
    public async Task<Roles> AddEditRoles(Roles roles)
    {
        if (string.IsNullOrEmpty(roles.RolesId))
        {
            await _RolesRepository.AddOne(roles);
        }
        else
        {
            Task<Roles?> currentRole = _RolesRepository.FirstOrDefault(x => x.RolesId == roles.RolesId);
            if (currentRole != null)
            {

            }
        }
        return roles;
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
        return _mapper.Map<RoleModel>(role); ;
    }
    //Saving role and it's permission
    public async Task<RoleWithModuleAndPermissions> SaveRoleAndPermissions(RoleWithModuleAndPermissions roleWithModuleAndPermissions)
    {
        if (roleWithModuleAndPermissions == null)
        {
            return null;
        }

        Sanitizer.SanitizeProperties(roleWithModuleAndPermissions);
        Roles Roles = await AddEditRoles(new Roles
        {
            Titles = roleWithModuleAndPermissions.RoleTitle,
            RolesId = roleWithModuleAndPermissions.RoleId,
            Description = roleWithModuleAndPermissions.Description,
            UserRoles = roleWithModuleAndPermissions.UserRoles,
            HasAppAccess = roleWithModuleAndPermissions.HasAppAccess,
            UpdatedDate = DateTime.Now
        });
        foreach (ModuleRolePermissionsModel permission in roleWithModuleAndPermissions.RolePermissions)
        {
            permission.RolesId = Roles.RolesId;
            await saveRolePermission(new RolePermission
            {
                RolePermissionId = permission.RolePermissionId,
                ModulePermissionId = permission.ModulePermissionId,
                RolesId = permission.RolesId,
                HasAccess = permission.HasAccess
            });
        }
        roleWithModuleAndPermissions.RoleId = Roles.RolesId;
        return roleWithModuleAndPermissions;
    }
    //Get all roles with their permission
    public async Task<List<ModuleWithPermissionsModel>> GetRoleWithPermissions(string roleId, string companyId)
    {
        List<ModuleWithPermissionsModel> moduleWithPermissionsModel = new();
        Expression<Func<Roles, bool>> whereCondition = x => x.CompanyId == companyId && x.RolesId == roleId;
        Roles role = await _RolesRepository.FirstOrDefault(whereCondition);
        if (role != null)
        {
            IEnumerable<RolePermission> rolePermissions = await _rolePermissionRepository.GetAll(x => x.RolesId == role.RolesId);
            List<ModulePermission> modulePermissions = (await _modulePermissionRepository.GetAll()).ToList();
            List<Permission> permissions = (await _permissionRepository.GetAll()).ToList();
            List<Module> modules = (await _moduleRepository.GetAll()).ToList();
            var result = (from rp in rolePermissions
                          join mp in modulePermissions on rp.ModulePermissionId equals mp.ModulePermissionId
                          join m in modules on mp.ModuleId equals m.ModuleId
                          join p in permissions on mp.PermissionId equals p.PermissionId
                          where rp.RolesId == role.RolesId
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
                    RolesId = y.rolePermissions.RolesId,
                    HasAccess = y.rolePermissions.HasAccess,
                    PermissionConstant = y.permission.PermissionConstant
                }).ToList();
                moduleWithPermissionModel.Permissions = permissionsList;
                moduleWithPermissionsModel.Add(moduleWithPermissionModel);
            }
        }
        return moduleWithPermissionsModel;
    }
    public async Task<List<Roles>> AddDefaultRole(string companyId)
    {
        List<Roles> roles = _RolesRepository.Get(x => x.IsDefault && string.IsNullOrEmpty(x.CompanyId)).ToList();
        List<string> roleIds = roles.Select(x => x.RolesId).ToList();
        IEnumerable<RolePermission> rolePermissions = await _rolePermissionRepository.GetAll(x => roleIds.Contains(x.RolesId));
        foreach (Roles? role in roles)
        {
            string oldRoleId = role.RolesId;
            role.RolesId = Guid.NewGuid().ToString();
            role.CompanyId = companyId;
            role.IsDefault = false;
            role.CreatedDate = DateTime.Now;
            List<RolePermission> permissions = rolePermissions.Where(x => x.RolesId == oldRoleId).ToList();
            foreach (RolePermission? item in permissions)
            {
                item.CreatedDate = DateTime.Now;
                item.CompanyId = companyId;
                item.RolesId = role.RolesId;
                await _rolePermissionRepository.AddOne(item);
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
        Expression<Func<User, bool>> whereUserCondtion = x => roleIds.Contains(x.RoleId);
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
        IEnumerable<RolePermission> rolePermissions = await _rolePermissionRepository.GetAll(x => rolesIds.Contains(x.RolesId));
        return (from role in roles
                join rp in rolePermissions on role.RolesId equals rp.RolesId
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

        RolePermission rolePermission = await _rolePermissionRepository.FirstOrDefault(x => x.RolesId == permission.RolesId && x.RolePermissionId == permission.RolePermissionId);
        if (rolePermission != null)
        {
            FilterDefinition<RolePermission> filter = Builders<RolePermission>.Filter.Where(e => e.RolesId == permission.RolesId && e.RolePermissionId == permission.RolePermissionId);
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
                RolesId = permission.RolesId,
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
    public async Task<bool> VerifyUserAccess(string module, string[] Role, string userId, string companyId, UserEditRoleCheckModel userForEdit = null)
    {
        bool hasPermission = false;
        string[] modules = Array.Empty<string>();
        if (userForEdit != null && !string.IsNullOrEmpty(userForEdit.UserId) && Role.Contains("Constants.Colleagues_Edit") && userId == userForEdit.UserId)
        {
            UserModel users = _middleware.GetUserById(userForEdit.UserId);
            if (users != null && users.RoleId == userForEdit.RoleId)
                return true;
        }

        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(companyId))

        {
            UserModel user = _middleware.GetUserById(userId);
            List<ModuleWithPermissionsModel> modulePremissions = await GetRoleWithPermissions(user.RoleId, companyId);
            List<ModuleWithPermissionsModel> userPermissionList = _mapper.Map<List<ModuleWithPermissionsModel>>(modulePremissions);

            if (!string.IsNullOrEmpty(module))
                modules = new string[] { module };

            for (int j = 0; j < modules.Length; j++)
            {
                module = modules[j];
                ModuleWithPermissionsModel? userPermission = userPermissionList.FirstOrDefault(x => module == x.ModuleConstant);
                //j==0 will determine that employee has access app as manager or Hr 
                if (userPermission != null && (j == 0 || hasPermission))
                {
                    for (int i = 0; i < userPermission.Permissions.Count; i++)
                    {
                        hasPermission = Role.Contains(userPermission.Permissions[i].PermissionConstant);
                        if (hasPermission)
                            break;
                    }
                }
                else
                    hasPermission = false;
            }

        }

        return hasPermission;
    }

}

