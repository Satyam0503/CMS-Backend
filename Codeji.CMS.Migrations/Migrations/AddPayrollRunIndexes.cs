using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;
namespace Codeji.CMS.Migrations.Migrations;
public sealed class AddPayrollRunIndexes : IMigration
{
 public string Id => $"2026-09-29-{nameof(AddPayrollRunIndexes)}";
 public async Task ExecuteAsync(IMongoDatabase db)
 {
  var migrations=db.GetCollection<Migration>("Migration"); if(await migrations.Find(x=>x.Id==Id).AnyAsync()) return;
  await Create(db,"PayrollRun","ux_payrollrun_company_period_type_population",true,"CompanyId","PeriodStart","RunType","PopulationType","DepartmentId","PayGroupId");
  await Create(db,"PayrollRun","ux_payrollrun_company_idempotency",true,"CompanyId","IdempotencyKey");
  await Create(db,"PayrollRunEmployee","ux_payrollrunemployee_company_run_employee",true,"CompanyId","PayrollRunId","EmployeeId");
  await migrations.InsertOneAsync(new Migration{Id=Id,ExecutedAt=DateTime.UtcNow});
 }
 static Task Create(IMongoDatabase db,string collection,string name,bool unique,params string[] fields)=>db.GetCollection<BsonDocument>(collection).Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument(fields.Select(x=>new BsonElement(x,1))),new CreateIndexOptions{Name=name,Unique=unique,Sparse=name.Contains("idempotency",StringComparison.OrdinalIgnoreCase)}));
}
