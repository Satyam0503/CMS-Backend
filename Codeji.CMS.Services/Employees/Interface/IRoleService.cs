using Codeji.CMS.DTO.RolePermissions;
namespace Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Repository.Entities.RolePermissions;
public interface IRoleService
{
    Task<Roles> AddEditRoles(Roles roles);
    Task<List<RoleModel>> GetRoles(string companyId);
    Task<RoleModel> GetRoleById(string roleId);
    Task<RoleWithModuleAndPermissions> SaveRoleAndPermissions(RoleWithModuleAndPermissions roleWithModuleAndPermissions);
    Task<List<ModuleWithPermissionsModel>> GetRoleWithPermissions(string roleId, string companyId);
    Task<List<Roles>> AddDefaultRole(string companyId);
    Task<List<ModuleWithPermissionsModel>> GetDefaultRoleWithPermissions(bool isEditableUserRole, string companyId);
    Task<List<RoleModel>> GetRolesWithPagination(int pageNo, int pageSize);
    Task<bool> CheckRoleDependancyForDeletion(string roleId);
    Task<List<string>> GetUsersByRole(string[] roleIds, string companyId);
    Task<bool> VerifyUserAccess(string module, string[] Role, string userId, string companyId, UserEditRoleCheckModel userForEdit = null);

}