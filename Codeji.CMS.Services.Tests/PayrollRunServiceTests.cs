using System.Linq.Expressions;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using Moq;
namespace Codeji.CMS.Services.Tests;
public sealed class PayrollRunServiceTests
{
 [Fact] public async Task Create_IsTenantScopedAndSnapshotsSelectedEmployees()
 {
  var runs=Repo(Array.Empty<PayrollRun>()); var lines=Repo(Array.Empty<PayrollRunEmployee>());
  var employees=Repo(new[]{new EmpUser{CompanyId="c1",EmployeeId="e1",UserId="u1",FirstName="A",LastName="B",Email="a@x"},new EmpUser{CompanyId="c2",EmployeeId="e2",UserId="u2",FirstName="C",LastName="D",Email="c@x"}});
  var service=new PayrollRunService(runs.Object,lines.Object,employees.Object,Repo(Array.Empty<PayrollDivisorPolicy>()).Object);
  var result=await service.Create("c1","actor",new CreatePayrollRunRequestDto{PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,9,30),PayDate=new DateTime(2026,10,1),PopulationType="SELECTED",EmployeeIds=["e1","e2"],IdempotencyKey="key-1"});
  Assert.True(result.Success); Assert.Equal(201,result.StatusCode); Assert.Equal("c1",result.MethodResult.CompanyId); Assert.Equal(1,result.MethodResult.EmployeeCount);
  lines.Verify(x=>x.AddMany(It.Is<IEnumerable<PayrollRunEmployee>>(v=>v.Count()==1&&v.Single().CompanyId=="c1"&&v.Single().EmployeeId=="e1")),Times.Once);
 }
 [Fact] public async Task Create_IsIdempotentForSameTenantKey()
 {
  var existing=new PayrollRun{Id="run-1",CompanyId="c1",PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,9,30),PayDate=new DateTime(2026,10,1),IdempotencyKey="same",CreatedAtUtc=DateTime.UtcNow};
  var runs=Repo(new[]{existing}); var lines=Repo(Array.Empty<PayrollRunEmployee>()); var employees=Repo(Array.Empty<EmpUser>()); var service=new PayrollRunService(runs.Object,lines.Object,employees.Object,Repo(Array.Empty<PayrollDivisorPolicy>()).Object);
  var result=await service.Create("c1","actor",new CreatePayrollRunRequestDto{PeriodStart=existing.PeriodStart,PeriodEnd=existing.PeriodEnd,PayDate=existing.PayDate,IdempotencyKey="same"});
  Assert.True(result.Success); Assert.Equal("run-1",result.ResourceId); runs.Verify(x=>x.AddOne(It.IsAny<PayrollRun>()),Times.Never);
 }
 [Fact] public async Task Create_RejectsSelectedPopulationWithoutEmployees()
 {
  var service=new PayrollRunService(Repo(Array.Empty<PayrollRun>()).Object,Repo(Array.Empty<PayrollRunEmployee>()).Object,Repo(Array.Empty<EmpUser>()).Object,Repo(Array.Empty<PayrollDivisorPolicy>()).Object);
  var result=await service.Create("c1","actor",new CreatePayrollRunRequestDto{PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,9,30),PayDate=new DateTime(2026,10,1),PopulationType="SELECTED"});
  Assert.False(result.Success); Assert.Equal(400,result.StatusCode);
 }
 static Mock<IMongoDbRepository<T>> Repo<T>(IEnumerable<T> items)
 { var a=items.ToArray(); var m=new Mock<IMongoDbRepository<T>>(); m.Setup(x=>x.Exist(It.IsAny<Expression<Func<T,bool>>>(),It.IsAny<bool>())).Returns((Expression<Func<T,bool>> p,bool _)=>Task.FromResult(a.Any(p.Compile()))); m.Setup(x=>x.FirstOrDefault(It.IsAny<Expression<Func<T,bool>>>(),It.IsAny<bool>(),It.IsAny<bool>())).Returns((Expression<Func<T,bool>> p,bool _,bool _)=>Task.FromResult(a.FirstOrDefault(p.Compile()))); m.Setup(x=>x.GetAll(It.IsAny<Expression<Func<T,bool>>>(),It.IsAny<bool>(),It.IsAny<bool>())).Returns((Expression<Func<T,bool>> p,bool _,bool _)=>Task.FromResult<IEnumerable<T>>(a.Where(p.Compile()).ToArray())); m.Setup(x=>x.AddOne(It.IsAny<T>())).Callback<T>(x=>{if(x is PayrollRun r&&string.IsNullOrEmpty(r.Id))r.Id="new-run";}).ReturnsAsync(new Codeji.CMS.Domain.Models.Result{Success=true}); m.Setup(x=>x.AddMany(It.IsAny<IEnumerable<T>>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result{Success=true}); return m; }
}
