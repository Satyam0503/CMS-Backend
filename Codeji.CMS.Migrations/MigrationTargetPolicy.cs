using Codeji.CMS.Migrations.Interface;

namespace Codeji.CMS.Migrations;

public static class MigrationTargetPolicy
{
    public static bool ShouldWithhold(IMigration migration, MigrationTarget target) =>
        migration is IDataBackfillMigration && (target is MigrationTarget.Development or MigrationTarget.Uat);
}
