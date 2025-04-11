using Codeji.CMS.DTO.RolePermissions;
namespace Codeji.CMS.Services.Employees.Interface;

using System.Collections.Generic;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.Repository.Entities.RolePermissions;
public interface IRoleService
{
    Task<string> AddEditRoles(RoleWithModuleAndPermissions roles, string companyId);
    Task<List<RoleModel>> GetRoles(string companyId);
    Task<RoleModel> GetRoleById(string roleId);
    //Task<RoleWithModuleAndPermissions> SaveRoleAndPermissions(RoleWithModuleAndPermissions roleWithModuleAndPermissions);
    Task<List<ModuleWithPermissionsModel>> GetRoleWithPermissions(string roleId, string companyId);
    Task<string[]> GetRolePermissionOfuser(string roleId);
    Task<List<Roles>> AddDefaultRole(string companyId);
    Task<List<ModuleWithPermissionsModel>> GetDefaultRoleWithPermissions(bool isEditableUserRole, string companyId);
    Task<List<RoleModel>> GetRolesWithPagination(int pageNo, int pageSize);
    Task<bool> CheckRoleDependancyForDeletion(string roleId);
    Task<List<string>> GetUsersByRole(string[] roleIds, string companyId);
    Task<bool> VerifyUserAccess(string module, string[] Role, string userId, string companyId, UserCheckModel userForEdit = null);
    Task<List<ModuleWithPermissionsModel>> GetAllRolesWithPermission(string companyId);
    Task<Result> UpdateAppAccessForRole(string roleId,bool hasAppAccess);
}