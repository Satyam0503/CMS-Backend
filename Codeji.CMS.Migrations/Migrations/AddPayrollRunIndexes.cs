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
  await CreateIdempotencyIndex(db);
  await Create(db,"PayrollRunEmployee","ux_payrollrunemployee_company_run_employee",true,"CompanyId","PayrollRunId","EmployeeId");
  await migrations.InsertOneAsync(new Migration{Id=Id,ExecutedAt=DateTime.UtcNow});
 }
 static Task Create(IMongoDatabase db,string collection,string name,bool unique,params string[] fields)
 {
  var options=new CreateIndexOptions<BsonDocument>{Name=name,Unique=unique};
  return db.GetCollection<BsonDocument>(collection).Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument(fields.Select(x=>new BsonElement(x,1))),options));
 }

 static async Task CreateIdempotencyIndex(IMongoDatabase db)
 {
  const string name="ux_payrollrun_company_idempotency";
  var collection=db.GetCollection<BsonDocument>("PayrollRun");
  var nonEmptyKey=new BsonDocument("IdempotencyKey",new BsonDocument{{"$type","string"},{"$gt",""}});
  var duplicate=await collection.Aggregate().Match(nonEmptyKey)
   .Group(new BsonDocument{{"_id",new BsonDocument{{"CompanyId","$CompanyId"},{"IdempotencyKey","$IdempotencyKey"}}},{"count",new BsonDocument("$sum",1)}})
   .Match(new BsonDocument("count",new BsonDocument("$gt",1))).FirstOrDefaultAsync();
  if(duplicate is not null) throw new InvalidOperationException("Payroll runs contain duplicate non-empty company/idempotency keys. Resolve duplicates before applying the idempotency index.");

  using var cursor=await collection.Indexes.ListAsync();
  var indexes=await cursor.ToListAsync();
  var current=indexes.FirstOrDefault(index=>index.GetValue("name","").AsString==name);
  if(current is not null&&!current.GetValue("partialFilterExpression",BsonNull.Value).Equals(nonEmptyKey))
  {
   await collection.Indexes.DropOneAsync(name);
   current=null;
  }
  if(current is null)
   await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument{{"CompanyId",1},{"IdempotencyKey",1}},new CreateIndexOptions<BsonDocument>{Name=name,Unique=true,PartialFilterExpression=nonEmptyKey}));
 }
}
