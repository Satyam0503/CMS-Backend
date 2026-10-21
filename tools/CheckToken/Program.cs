using MongoDB.Bson;
using MongoDB.Driver;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Domain.Models;

var configPath = @"d:\CMS\CMS-Backend-Core\Codeji.CMS.API\appsettings.json";
using var config = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
var connection = config.RootElement.GetProperty("ConnectionStrings").GetProperty("mongodb").GetString();
ConfigManager.AppSettings = new AppSettings
{
    APIUrl = "http://localhost:5036/",
    AppUrl = "http://localhost:5173/",
    AppVersion = "v4.0.0.15102024"
};
ConfigManager.Jwt = new JwtSettings
{
    SecretKey = config.RootElement.GetProperty("Jwt").GetProperty("SecretKey").GetString() ?? "",
    Expiry = 1440
};

var db = new MongoClient(connection).GetDatabase(MongoUrl.Create(connection).DatabaseName);
var users = db.GetCollection<BsonDocument>("EmpUser");
var anchor = await users.Find(new BsonDocument("Email", new BsonRegularExpression(@"^\s*satyam@codeji\.in\s*$", "i"))).FirstOrDefaultAsync();

if (anchor == null)
{
    Console.WriteLine("User not found!");
    return;
}

var userId = anchor.TryGetValue("UserId", out var u) ? u.ToString() : anchor["_id"].ToString();
var companyId = anchor["CompanyId"].AsString;
var roleId = anchor["RoleId"].AsString;

var roles = db.GetCollection<BsonDocument>("Roles");
var roleDoc = await roles.Find(new BsonDocument("RolesId", roleId)).FirstOrDefaultAsync();
var roleTitle = roleDoc != null ? roleDoc.GetValue("Titles", "Administrator").AsString : "Administrator";

var token = AuthenticationHandler.GenerateJwtToken(userId, companyId, roleId, new List<string> { roleTitle });
var refreshToken = Guid.NewGuid().ToString("N");

var refreshTokens = db.GetCollection<BsonDocument>("RefreshToken");
var hashed = TokenHelper.ComputeSha256Hash(refreshToken);
await refreshTokens.InsertOneAsync(new BsonDocument
{
    { "_id", Guid.NewGuid().ToString() },
    { "UserId", userId },
    { "Token", hashed },
    { "ExpireAt", DateTime.UtcNow.AddDays(7) },
    { "CreatedAt", DateTime.UtcNow }
});

Console.WriteLine($"TOKEN:{token}");
Console.WriteLine($"REFRESH:{refreshToken}");
Console.WriteLine($"USERID:{userId}");
Console.WriteLine($"COMPANYID:{companyId}");
