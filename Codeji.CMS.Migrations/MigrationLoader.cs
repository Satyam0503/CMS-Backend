using System.Reflection;
using Codeji.CMS.Migrations.Interface;

namespace Codeji.CMS.Migrations;

public static class MigrationLoader
{
    public static List<IMigration> LoadMigrations()
    {
        var migrationType = typeof(IMigration);

        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t =>
                migrationType.IsAssignableFrom(t) &&
                !t.IsAbstract &&
                !t.IsInterface)
            .Select(t => (IMigration)Activator.CreateInstance(t)!)
            .OrderBy(m => m.Id)
            .ToList();
    }
}
