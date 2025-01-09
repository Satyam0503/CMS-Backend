using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualBasic;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Data;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using Module = Codeji.CMS.Repository.Entities.RolePermissions.Module;

namespace Codeji.CMS.Services;
public class RoleServices : IRoleService
{
    private readonly IMongoDbRepository<CompanyRole> _companyRoleRepository;
    private readonly IMongoDbRepository<RolePermission> _rolePermissionRepository;
    private readonly IMongoDbRepository<ModulePermission> _modulePermissionRepository;
    private readonly IMongoDbRepository<Module> _moduleRepository;
    private readonly IMongoDbRepository<Permission> _permissionRepository;
    private readonly IMongoDbRepository<User> _userRepository;
    private readonly IMapper _mapper;
    private readonly IMiddlewareService _middleware;

    public RoleServices(
        IMongoDbRepository<CompanyRole> companyRoleRepository,
        IMongoDbRepository<RolePermission> rolePermissionRepository,
        IMongoDbRepository<ModulePermission> modulePermissionRepository,
        IMongoDbRepository<Module> moduleRepository,
        IMongoDbRepository<Permission> permissionRepository,
        IMongoDbRepository<User> userRepository,
        IMapper mapper,
        IMiddlewareService middleware)
    {
        _companyRoleRepository = companyRoleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _modulePermissionRepository = modulePermissionRepository;
        _moduleRepository = moduleRepository;
        _permissionRepository = permissionRepository;
        _userRepository = userRepository;
        _mapper = mapper;
        _middleware = middleware;
    }

    //Add New Roles
    public async Task<RoleModel> AddEditRoles(RoleModel roles)
    {
        if (string.IsNullOrEmpty(roles.CompanyRoleId))
        {
            var newRole = new CompanyRole()
            {
                Titles = roles.Titles,
                Description = roles.Description,
                CreatedDate = DateTime.UtcNow,
            };
            await _companyRoleRepository.AddOne(newRole);
        }
        else
        {
            var currentRole = _companyRoleRepository.FirstOrDefault(x => x.CompanyRoleId == roles.CompanyRoleId);
            if (currentRole != null)
            {

            }
        }
        return roles;
    }
    //Get company's all roles
    public async Task<List<RoleModel>> GetRoles(string companyId)
    {
        List<CompanyRole> roles = (await _companyRoleRepository.GetAll(x => x.CompanyId == companyId)).ToList();
        return await returnRolesList(roles);
    }
    //Fetch Default roles of company
    public async Task<RoleModel> GetDefaultRole(string RoleName, string companyId, bool isRoleEditable)
    {
        CompanyRole role = await fetchRoleData(x => x.CompanyId == companyId && x.Titles == RoleName.ToLower() && x.IsNotEditable == isRoleEditable);
        return _mapper.Map<RoleModel>(role); ;
    }
    //Fetch matched role with given Id
    public async Task<RoleModel> GetRoleById(string roleId)
    {
        CompanyRole role = await fetchRoleData(x => x.CompanyRoleId == roleId);
        return _mapper.Map<RoleModel>(role); ;
    }
    private async Task<CompanyRole> SingleOrDefault(Expression<Func<CompanyRole, bool>> filter)
    {
        return await _companyRoleRepository.FirstOrDefault(filter);
    }
    private async Task<CompanyRole> SaveRole(CompanyRole roleModel)
    {
        if (roleModel != null)
        {
            CompanyRole companyRole = await SingleOrDefault(x => x.CompanyRoleId == roleModel.CompanyRoleId);
            if (companyRole != null)
            {
                FilterDefinition<CompanyRole> filter = Builders<CompanyRole>.Filter.Where(e => e.CompanyRoleId == companyRole.CompanyRoleId);
                companyRole.Titles = roleModel.Titles;
                companyRole.Description = roleModel.Description;
                companyRole.UserRoles = roleModel.UserRoles;
                await _companyRoleRepository.Update(filter, companyRole);
            }
            else
            {
                companyRole = new CompanyRole();
                companyRole.Titles = roleModel.Titles;
                companyRole.Description = roleModel.Description;
                companyRole.CompanyId = roleModel.CompanyId;
                companyRole.UserRoles = roleModel.UserRoles;
                await _companyRoleRepository.AddOne(companyRole);
            }
            return companyRole;
        }
        return null;
    }
    public async Task<bool> DeleteAndReassignRole(string companyId, DeleteAndReassignRoleRequestModel model)
    {
        Result result = new Result();
        Expression<Func<CompanyRole, bool>> whereCondition = x => (x.CompanyRoleId == model.RoleId || x.CompanyRoleId == model.NewRole) && x.CompanyId == companyId;
        Expression<Func<User, bool>> whereUserCondtion = x => x.CompanyId == companyId && x.RoleId == model.RoleId;
        Expression<Func<CompanyRole, bool>> deleteCondition = x => x.CompanyId == companyId && x.CompanyRoleId == model.RoleId;
        List<CompanyRole> Roles = (await _companyRoleRepository.GetAll(whereCondition)).ToList();
        if (Roles.Count > 1 && Roles.Exists(x => x.CompanyRoleId == model.RoleId))
        {
            result = await _companyRoleRepository.UpdateMany(deleteCondition, Builders<CompanyRole>.Update.Set(x => x.IsDeleted, true));

            await _userRepository.UpdateMany(whereUserCondtion, Builders<User>.Update.Set(x => x.RoleId, model.NewRole));
            return true;
        }
        return false;
    }
    //public bool RoleExists(string companyId, string roleId, string roleName, string selectedLanguage)
    public async Task<bool> RoleExists(string companyId, string roleId, string roleTitle, string selectedLanguage)
    {
        Expression<Func<CompanyRole, bool>> whereCondition = x => !x.IsNotEditable && (!string.IsNullOrEmpty(roleId) && x.CompanyRoleId != roleId) && x.Titles == roleTitle;
        var query = await _companyRoleRepository.Exist(whereCondition);
        return query;
    }
    //Saving role and it's permission
    public async Task<RoleWithModuleAndPermissions> SaveRoleAndPermissions(RoleWithModuleAndPermissions roleWithModuleAndPermissions)
    {
        if (roleWithModuleAndPermissions == null)
        {
            return null;
        }

        Sanitizer.SanitizeProperties(roleWithModuleAndPermissions);
        CompanyRole companyRole = await SaveRole(new CompanyRole
        {
            Titles = roleWithModuleAndPermissions.RoleTitle,
            CompanyRoleId = roleWithModuleAndPermissions.RoleId,
            Description = roleWithModuleAndPermissions.Description,
            UserRoles = roleWithModuleAndPermissions.UserRoles,
            UpdatedDate = DateTime.Now
        });
        foreach (ModuleRolePermissionsModel permission in roleWithModuleAndPermissions.RolePermissions)
        {
            permission.CompanyRoleId = companyRole.CompanyRoleId;
            await saveRolePermission(new RolePermission
            {
                RolePermissionId = permission.RolePermissionId,
                ModulePermissionId = permission.ModulePermissionId,
                CompanyRoleId = permission.CompanyRoleId,
                HasAccess = permission.HasAccess
            });
        }
        roleWithModuleAndPermissions.RoleId = companyRole.CompanyRoleId;
        return roleWithModuleAndPermissions;
    }
    //Get all roles with their permission
    public async Task<List<ModuleWithPermissionsModel>> GetRoleWithPermissions(string roleId, string companyId)
    {
        List<ModuleWithPermissionsModel> moduleWithPermissionsModel = new();
        Expression<Func<CompanyRole, bool>> whereCondition = x => x.CompanyId == companyId && x.CompanyRoleId == roleId;
        CompanyRole role = await fetchRoleData(whereCondition);
        if (role != null)
        {
            var rolePermissions = await _rolePermissionRepository.GetAll(x => x.CompanyRoleId == role.CompanyRoleId);
            List<ModulePermission> modulePermissions = (await _modulePermissionRepository.GetAll()).ToList();
            List<Permission> permissions = (await _permissionRepository.GetAll()).ToList();
            List<Module> modules = (await _moduleRepository.GetAll()).ToList();
            var result = (from rp in rolePermissions
                          join mp in modulePermissions on rp.ModulePermissionId equals mp.ModulePermissionId
                          join m in modules on mp.ModuleId equals m.ModuleId
                          join p in permissions on mp.PermissionId equals p.PermissionId
                          where rp.CompanyRoleId == role.CompanyRoleId
                          select new { permission = p, rolePermissions = rp, module = m, modulePermission = mp }).ToList();
            var companyRoles = result.Select(x => new { x.module.ModuleId, x.module.ModuleName, x.module.ModuleConstant }).Distinct().ToArray();
            for (int i = 0; i < companyRoles.Length; i++)
            {
                var companyRole = companyRoles[i];
                ModuleWithPermissionsModel moduleWithPermissionModel = new()
                {
                    ModuleName = companyRole.ModuleName,
                    ModuleConstant = companyRole.ModuleConstant
                };
                List<ModuleRolePermissionsModel> permissionsList = result.Where(x => x.module.ModuleId == companyRole.ModuleId).Select(y => new ModuleRolePermissionsModel
                {
                    PermissionName = y.permission.PermissionName,
                    RolePermissionId = y.rolePermissions.RolePermissionId,
                    ModulePermissionId = y.modulePermission.ModulePermissionId,
                    CompanyRoleId = y.rolePermissions.CompanyRoleId,
                    HasAccess = y.rolePermissions.HasAccess,
                    PermissionConstant = y.permission.PermissionConstant
                }).ToList();
                moduleWithPermissionModel.Permissions = permissionsList;
                moduleWithPermissionsModel.Add(moduleWithPermissionModel);
            }
        }
        return moduleWithPermissionsModel;
    }
    //Get default roles with their permission
    public async Task<List<ModuleWithPermissionsModel>> GetDefaultRoleWithPermissions(bool isEditableUserRole, string companyId)
    {
        List<ModuleWithPermissionsModel> moduleWithPermissionsModel = new();
        Expression<Func<CompanyRole, bool>> whereCondition = x => x.CompanyId == companyId && x.IsNotEditable == true;
        CompanyRole role = await fetchRoleData(whereCondition);
        if (role != null)
        {
            moduleWithPermissionsModel = await GetRoleWithPermissions(role.CompanyRoleId, role.CompanyId);
            //int index = moduleWithPermissionsModel.FindIndex(x => x.ModuleConstant == Constants.Dashboard);
            //if (index > -1)
            //{
            //    moduleWithPermissionsModel[index].Permissions[0].HasAccess = false;
            //}
        }
        return moduleWithPermissionsModel;
    }
    //Get company's all roles
    public async Task<List<RoleModel>> GetRolesWithPagination(int pageNo, int pageSize)
    {
        int skiprecords = (pageNo - 1) * pageSize;
        Expression<Func<CompanyRole, bool>> whereCondition = x => true;
        List<CompanyRole> roles = (await _companyRoleRepository.GetAll()).ToList();
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
        Expression<Func<CompanyRole, bool>> whereCondition = x => x.CompanyRoleId == roleId && !x.IsNotEditable;
        CompanyRole Role = await _companyRoleRepository.FirstOrDefault(whereCondition);
        if (Role != null)
        {
            Role.IsDeleted = true;
            await _companyRoleRepository.Update(whereCondition, Role);
            result = true;
        }
        return result;
    }
    public async Task<List<string>> GetUsersByRole(string[] roleIds, string companyId)
    {
        Expression<Func<User, bool>> whereUserCondtion = x => roleIds.Contains(x.RoleId);
        var users = await _userRepository.Get(whereUserCondtion).Select(x => x.UserId).ToListAsync();
        return users;
    }

    #region Private Methods
    //Fetch role data from DB
    private async Task<CompanyRole> fetchRoleData(Expression<Func<CompanyRole, bool>> filter)
    {
        return await _companyRoleRepository.FirstOrDefault(filter);
    }
    //Save Company role
    private async Task<CompanyRole> saveRole(CompanyRole roleModel)
    {
        if (roleModel == null)
        {
            return null;
        }

        CompanyRole companyRole = await fetchRoleData(x => x.CompanyRoleId == roleModel.CompanyRoleId);
        if (companyRole != null)
        {
            FilterDefinition<CompanyRole> filter = Builders<CompanyRole>.Filter.Where(e => e.CompanyRoleId == companyRole.CompanyRoleId);
            companyRole.Titles = roleModel.Titles;
            companyRole.Description = roleModel.Description;
            companyRole.UserRoles = roleModel.UserRoles;
            companyRole.UpdatedBy = "saveRole";
            companyRole.UpdatedDate = DateTime.UtcNow;
            await _companyRoleRepository.Update(filter, companyRole);
        }
        else
        {
            companyRole = new CompanyRole()
            {
                Titles = roleModel.Titles,
                Description = roleModel.Description,
                CompanyId = roleModel.CompanyId,
                UserRoles = roleModel.UserRoles,
                CreatedBy = "saveRole",
                CreatedDate = DateTime.UtcNow,
            };
            await _companyRoleRepository.AddOne(companyRole);
        }
        return companyRole;
    }
    //Transform Company role with permission
    private async Task<List<RoleModel>> returnRolesList(List<CompanyRole> roles)
    {
        // for roles connection to the application permission change
        string[] rolesIds = roles.Select(x => x.CompanyRoleId).ToArray();
        // 27 for Dashboard acces permission.
        IEnumerable<RolePermission> rolePermissions = await _rolePermissionRepository.GetAll(x => rolesIds.Contains(x.CompanyRoleId));
        return (from role in roles
                join rp in rolePermissions on role.CompanyRoleId equals rp.CompanyRoleId
                select new RoleModel
                {
                    CompanyRoleId = role.CompanyRoleId,
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

        RolePermission rolePermission = await _rolePermissionRepository.FirstOrDefault(x => x.CompanyRoleId == permission.CompanyRoleId && x.RolePermissionId == permission.RolePermissionId);
        if (rolePermission != null)
        {
            FilterDefinition<RolePermission> filter = Builders<RolePermission>.Filter.Where(e => e.CompanyRoleId == permission.CompanyRoleId && e.RolePermissionId == permission.RolePermissionId);
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
                CompanyRoleId = permission.CompanyRoleId,
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

