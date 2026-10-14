using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

/// <summary>
/// Enforces enterprise least-privilege payroll isolation:
/// - Revokes all PayRoll and PayrollSettings permissions from regular employees and non-HR/non-Admin roles.
/// - Ensures Administrator, HR Manager, and HR Executive retain full authorized payroll management access.
/// - Guarantees regular employees can only access their personal payslip via self-service.
/// </summary>
public sealed class HardenPayrollRolePermissions : IMigration
{
    public string Id => $"2026-10-13-{nameof(HardenPayrollRolePermissions)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>(nameof(Migration));
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var modulesCol = db.GetCollection<Module>(nameof(Module));
        var modulePermissionsCol = db.GetCollection<ModulePermission>(nameof(ModulePermission));
        var rolesCol = db.GetCollection<Roles>(nameof(Roles));
        var rolePermissionsCol = db.GetCollection<RolePermission>(nameof(RolePermission));

        var payrollModules = await modulesCol.Find(m =>
            m.ModuleConstant == AppModule.PayRoll || m.ModuleConstant == AppModule.PayrollSettings)
            .ToListAsync();

        if (payrollModules.Count == 0) return;

        var payrollModuleIds = payrollModules.Select(m => m.ModuleId).ToList();
        var payrollModulePermissions = await modulePermissionsCol.Find(mp =>
            payrollModuleIds.Contains(mp.ModuleId))
            .ToListAsync();

        var payrollMpIds = payrollModulePermissions.Select(mp => mp.ModulePermissionId).ToList();
        if (payrollMpIds.Count == 0) return;

        var allRoles = await rolesCol.Find(r => !r.IsDeleted).ToListAsync();

        var hrOrAdminRoleTypes = new[]
        {
            (int)EnumsHelper.Roles.Administrator,
            (int)EnumsHelper.Roles.HR,
            (int)EnumsHelper.Roles.HRExecutive,
        };

        var nonHrRoleIds = allRoles
            .Where(r => !hrOrAdminRoleTypes.Contains(r.RoleType))
            .Select(r => r.RolesId)
            .ToList();

        // 1. Revoke PayRoll and PayrollSettings from all non-management roles across all companies
        if (nonHrRoleIds.Count > 0)
        {
            await rolePermissionsCol.DeleteManyAsync(rp =>
                nonHrRoleIds.Contains(rp.RoleId) &&
                payrollMpIds.Contains(rp.ModulePermissionId));
        }

        // 2. Ensure HR and Admin roles have active permissions for payroll management
        var managementRoles = allRoles
            .Where(r => hrOrAdminRoleTypes.Contains(r.RoleType))
            .ToList();

        foreach (var role in managementRoles)
        {
            foreach (var mpId in payrollMpIds)
            {
                var existing = await rolePermissionsCol.Find(rp =>
                    rp.RoleId == role.RolesId &&
                    rp.CompanyId == role.CompanyId &&
                    rp.ModulePermissionId == mpId)
                    .FirstOrDefaultAsync();

                if (existing == null)
                {
                    await rolePermissionsCol.InsertOneAsync(new RolePermission
                    {
                        RoleId = role.RolesId,
                        CompanyId = role.CompanyId,
                        ModulePermissionId = mpId,
                        HasAccess = true,
                        IsAccessible = true,
                        CreatedDate = DateTime.UtcNow,
                    });
                }
                else if (!existing.HasAccess || !existing.IsAccessible)
                {
                    await rolePermissionsCol.UpdateOneAsync(
                        rp => rp.RolePermissionId == existing.RolePermissionId,
                        Builders<RolePermission>.Update
                            .Set(rp => rp.HasAccess, true)
                            .Set(rp => rp.IsAccessible, true));
                }
            }
        }

        await migrations.InsertOneAsync(new Migration
        {
            Id = Id,
            ExecutedAt = DateTime.UtcNow,
        });
    }
}

