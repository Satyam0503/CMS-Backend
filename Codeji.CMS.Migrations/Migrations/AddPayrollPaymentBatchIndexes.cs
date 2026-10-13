using Codeji.CMS.Migrations.Interface;
using Codeji.CMS.Repository.Entities;
using MongoDB.Bson;
using MongoDB.Driver;
namespace Codeji.CMS.Migrations.Migrations;
public sealed class AddPayrollPaymentBatchIndexes : IMigration
{
 public string Id => "2026-09-29-AddPayrollPaymentBatchIndexes";
 public async Task ExecuteAsync(IMongoDatabase db){var m=db.GetCollection<Migration>("Migration");if(await m.Find(x=>x.Id==Id).AnyAsync())return;await db.GetCollection<BsonDocument>("PayrollPaymentBatch").Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument(new[]{new BsonElement("CompanyId",1),new BsonElement("PayrollRunId",1)}),new CreateIndexOptions{Name="ux_payrollpaymentbatch_company_run",Unique=true}));await m.InsertOneAsync(new Migration{Id=Id,ExecutedAt=DateTime.UtcNow});}
}