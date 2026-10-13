using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Migrations.Migrations;

public sealed class AddCompanyRegistrationEmailIndex : IMigration
{
    public string Id => $"2026-09-24-{nameof(AddCompanyRegistrationEmailIndex)}";

    public async Task ExecuteAsync(IMongoDatabase db)
    {
        var migrations = db.GetCollection<Migration>("Migration");
        if (await migrations.Find(x => x.Id == Id).AnyAsync()) return;

        var companies = db.GetCollection<BsonDocument>("Company");
        var employees = db.GetCollection<BsonDocument>("EmpUser");
        var companyRows = await companies.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var employeeRows = await employees.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var emailByUserId = employeeRows
            .Where(row => row.TryGetValue("_id", out var id) && id.IsString)
            .Select(row => new
            {
                UserId = row["_id"].AsString,
                Email = Normalize(row.GetValue("EmailNormalized", row.GetValue("Email", "")).ToString())
            })
            .Where(row => row.Email.Length > 0)
            .GroupBy(row => row.UserId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Email, StringComparer.Ordinal);

        var registrations = new List<(BsonValue CompanyId, string Email, bool Active)>();
        foreach (var company in companyRows)
        {
            var normalized = Normalize(company.GetValue("RegistrationEmailNormalized", "").ToString());
            if (normalized.Length == 0)
            {
                var primaryContact = company.GetValue("PrimaryContact", "").ToString();
                if (primaryContact.Length > 0 &&
                    emailByUserId.TryGetValue(primaryContact, out var primaryEmail))
                    normalized = primaryEmail;
            }

            if (string.IsNullOrWhiteSpace(normalized)) continue;
            registrations.Add((
                company["_id"],
                normalized,
                company.GetValue("Status", false).ToBoolean()));
        }

        var duplicates = registrations
            .GroupBy(row => row.Email, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(email => email)
            .ToList();
        if (duplicates.Count > 0)
            throw new InvalidOperationException(
                $"Cannot create company registration-email uniqueness index. Resolve duplicate registration emails first: {string.Join(", ", duplicates.Take(20))}");

        foreach (var registration in registrations)
        {
            var update = Builders<BsonDocument>.Update
                .Set("RegistrationEmailNormalized", registration.Email)
                .Set("ProvisioningState", registration.Active ? "Active" : "Failed");
            await companies.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", registration.CompanyId),
                update);
        }

        var partial = new BsonDocument
        {
            { "RegistrationEmailNormalized", new BsonDocument { { "$exists", true }, { "$type", "string" }, { "$gt", "" } } },
            { "IsDeleted", false }
        };
        await companies.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            new BsonDocument("RegistrationEmailNormalized", 1),
            new CreateIndexOptions<BsonDocument>
            {
                Name = "ux_company_registration_email_normalized",
                Unique = true,
                PartialFilterExpression = partial
            }));

        await migrations.InsertOneAsync(new Migration { Id = Id, ExecutedAt = DateTime.UtcNow });
    }

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();
}