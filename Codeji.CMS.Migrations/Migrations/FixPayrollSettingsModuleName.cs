using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Utility.Constraints;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

// AddPayrollSettingseAndItsModulePermissions originally set the Payroll Settings module's
// display name to the raw constant ("Payroll_Settings") instead of a readable label, so it
// showed up wrong (or was easy to miss) in the Roles & Permissions screen. This corrects the
// already-seeded document for environments where that migration already ran.
public class FixPayrollSettingsModuleName : IMigration
{
    public string Id => $"2026-07-20-{typeof(FixPayrollSettingsModuleName).Name}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(m => m.Id == Id).AnyAsync()) return;

        var modules = db.GetCollection<Module>(typeof(Module).Name);
        await modules.UpdateOneAsync(
            m => m.ModuleConstant == AppModule.PayrollSettings,
            Builders<Module>.Update.Set(m => m.ModuleName, "Payroll Settings"));

        await migrations.InsertOneAsync(new Migration
        {
            Id = Id,
            ExecutedAt = DateTime.UtcNow,
        });
    }
}
