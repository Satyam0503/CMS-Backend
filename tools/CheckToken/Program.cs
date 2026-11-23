using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Codeji.CMS.Utility.Helpers;

var configPath = @"d:\CMS\CMS-Backend-Core\Codeji.CMS.API\appsettings.json";
using var config = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
var connection = config.RootElement.GetProperty("ConnectionStrings").GetProperty("mongodb").GetString();
var db = new MongoClient(connection).GetDatabase(MongoUrl.Create(connection).DatabaseName);

var userCol = db.GetCollection<BsonDocument>("EmpUser");

var hashedPassword = AuthenticationHandler.HashedPassword("Admin@123456");

// 1. Update existing admin bhatt.saurabh92@gmail.com
var updateResult = await userCol.UpdateOneAsync(
    new BsonDocument { { "Email", "bhatt.saurabh92@gmail.com" } },
    Builders<BsonDocument>.Update
        .Set("Password", hashedPassword)
        .Set("Status", true)
        .Set("IsEmailVerified", true)
);
Console.WriteLine($"Updated bhatt.saurabh92@gmail.com: {updateResult.ModifiedCount} modified");

// 2. Also ensure admin@codeji.in exists with Admin@123456
var existingAdmin = await userCol.Find(new BsonDocument { { "Email", "admin@codeji.in" } }).FirstOrDefaultAsync();
if (existingAdmin is null)
{
    var newAdmin = new BsonDocument
    {
        { "_id", Guid.NewGuid().ToString() },
        { "CompanyId", "e41e89e3-8459-495f-a7ca-bfe2b8c1e369" },
        { "RoleId", "8c09a3ed-460a-452e-aaae-9b6c58f7793a" },
        { "FirstName", "Codeji" },
        { "LastName", "Admin" },
        { "Email", "admin@codeji.in" },
        { "EmailNormalized", "admin@codeji.in" },
        { "Password", hashedPassword },
        { "Gender", "male" },
        { "EmployeeId", "EMP-ADM-001" },
        { "Status", true },
        { "IsEmailVerified", true },
        { "EmploymentStatus", "Active" },
        { "IsDeleted", false },
        { "CreatedDate", DateTime.UtcNow },
        { "Lifecycle", new BsonDocument
            {
                { "EmploymentStatus", "Active" },
                { "AvailabilityStatus", "Working" },
                { "AccountStatus", "Active" },
                { "PayrollStatus", "Active" },
                { "OffboardingStatus", "None" },
                { "FormerPortalStatus", "NotEligible" },
                { "AlumniStatus", "NotApplicable" },
                { "Version", 0 },
                { "History", new BsonArray() }
            }
        }
    };
    await userCol.InsertOneAsync(newAdmin);
    Console.WriteLine("Created admin@codeji.in with Admin@123456");
}
else
{
    await userCol.UpdateOneAsync(
        new BsonDocument { { "Email", "admin@codeji.in" } },
        Builders<BsonDocument>.Update
            .Set("Password", hashedPassword)
            .Set("Status", true)
            .Set("IsEmailVerified", true)
    );
    Console.WriteLine("Updated admin@codeji.in with Admin@123456");
}
