using Codeji.CMS.DTO;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.Domain.Models;
namespace Codeji.CMS.Services.Interface;
using Codeji.CMS.Repository.Entities.RolePermissions;
public interface IRoleService
{

    Task<List<RoleModel>> GetRoles(string companyId);
    Task<RoleModel> GetDefaultRole(string RoleName, string companyId, bool isRoleEditable);
    Task<RoleModel> GetRoleById(string roleId);
    Task<bool> DeleteAndReassignRole(string companyId, DeleteAndReassignRoleRequestModel model);
    Task<bool> RoleExists(string companyId, string roleId, string roleTitle, string selectedLanguage);
    Task<RoleWithModuleAndPermissions> SaveRoleAndPermissions(RoleWithModuleAndPermissions roleWithModuleAndPermissions);
    Task<List<ModuleWithPermissionsModel>> GetRoleWithPermissions(string roleId, string companyId);
    Task<List<ModuleWithPermissionsModel>> GetDefaultRoleWithPermissions(bool isEditableUserRole, string companyId);
    Task<List<RoleModel>> GetRolesWithPagination(int pageNo, int pageSize);
    Task<bool> CheckRoleDependancyForDeletion(string roleId);
    Task<List<string>> GetUsersByRole(string[] roleIds, string companyId);
    Task<bool> VerifyUserAccess(string module, string[] Role, string userId, string companyId, UserEditRoleCheckModel userForEdit = null);
    Task<RoleModel> AddEditRoles(RoleModel roles);
}