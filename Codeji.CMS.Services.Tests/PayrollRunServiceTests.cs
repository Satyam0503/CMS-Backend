using System.Linq.Expressions;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.DTO.Salary;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Interfaces;
using Codeji.CMS.Services.PayRoll;
using MongoDB.Driver;
using Moq;
namespace Codeji.CMS.Services.Tests;
public sealed class PayrollRunServiceTests
{
 [Fact] public async Task Create_RejectsCrossMonthPeriodBeforePersistingRun()
 {
  var runs=Repo(Array.Empty<PayrollRun>());
  var service=new PayrollRunService(runs.Object,Repo(Array.Empty<PayrollRunEmployee>()).Object,
   Repo(Array.Empty<EmpUser>()).Object,Repo(Array.Empty<PayrollDivisorPolicy>()).Object);

  var result=await service.Create("c1","actor",new CreatePayrollRunRequestDto
  {
   PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,10,31),PayDate=new DateTime(2026,11,1)
  });

  Assert.False(result.Success); Assert.Equal(400,result.StatusCode);
  Assert.Contains("exactly one calendar month",result.Message);
  runs.Verify(x=>x.AddOne(It.IsAny<PayrollRun>()),Times.Never);
 }

 [Fact] public async Task Create_FreezesTenantScopedLegacySalaryAsVersionZeroSnapshot()
 {
  var userId=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
  var employee=new EmpUser{CompanyId="c1",EmployeeId="e1",UserId=userId.ToString(),FirstName="A",LastName="B",Email="a@x",Status=true,DateOfJoining="2026-01-01"};
  var salary=new SalaryModel{SalaryId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),UserId=userId,CompanyId="c1",EmployeeId="e1",BasicPay=50000m,EffectiveFrom=new DateTime(2026,1,1),PaymentFrequency="MONTHLY",Currency="INR"};
  var legacy= new Mock<ISalaryRepository>();
  legacy.Setup(x=>x.GetSalaryHistoryAsync("c1",userId)).ReturnsAsync(new List<SalaryModel>{salary});
  var runs=Repo(Array.Empty<PayrollRun>()); var lineRepo=Repo(Array.Empty<PayrollRunEmployee>()); PayrollRunEmployee? savedLine=null;
  lineRepo.Setup(x=>x.AddMany(It.IsAny<IEnumerable<PayrollRunEmployee>>())).Callback<IEnumerable<PayrollRunEmployee>>(x=>savedLine=Assert.Single(x)).ReturnsAsync(new Codeji.CMS.Domain.Models.Result{Success=true});
  var service=new PayrollRunService(runs.Object,lineRepo.Object,Repo(new[]{employee}).Object,Repo(Array.Empty<PayrollDivisorPolicy>()).Object,
   employeeSalaryStructures:Repo(Array.Empty<EmployeeSalaryStructure>()).Object,legacySalaries:legacy.Object);

  var result=await service.Create("c1","actor",new CreatePayrollRunRequestDto{PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,9,30),PayDate=new DateTime(2026,10,1)});

  Assert.True(result.Success,result.Message);
  Assert.NotNull(savedLine);
  var snapshot=Assert.Single(savedLine!.InputSnapshot.SalaryStructures);
  Assert.Equal(salary.SalaryId.ToString("D"),snapshot.SalaryStructureId); Assert.Equal(0,snapshot.Version);
  Assert.Equal(50000m,Assert.Single(snapshot.Components.Where(x=>x.Code=="BASIC_PAY")).Value);
  legacy.Verify(x=>x.GetSalaryHistoryAsync("c1",userId),Times.Once);
 }

 [Fact] public async Task Create_IsTenantScopedAndSnapshotsSelectedEmployees()
 {
  var runs=Repo(Array.Empty<PayrollRun>()); var lines=Repo(Array.Empty<PayrollRunEmployee>());
  var employees=Repo(new[]{new EmpUser{CompanyId="c1",EmployeeId="e1",UserId="u1",FirstName="A",LastName="B",Email="a@x",Status=true,DateOfJoining="2026-01-01"},new EmpUser{CompanyId="c2",EmployeeId="e2",UserId="u2",FirstName="C",LastName="D",Email="c@x",Status=true,DateOfJoining="2026-01-01"}});
  var service=new PayrollRunService(runs.Object,lines.Object,employees.Object,Repo(Array.Empty<PayrollDivisorPolicy>()).Object);
  var result=await service.Create("c1","actor",new CreatePayrollRunRequestDto{PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,9,30),PayDate=new DateTime(2026,10,1),PopulationType="SELECTED",EmployeeIds=["e1"],IdempotencyKey="key-1"});
  Assert.True(result.Success); Assert.Equal(201,result.StatusCode); Assert.Equal("c1",result.MethodResult.CompanyId); Assert.Equal(1,result.MethodResult.EmployeeCount);
  lines.Verify(x=>x.AddMany(It.Is<IEnumerable<PayrollRunEmployee>>(v=>v.Count()==1&&v.Single().CompanyId=="c1"&&v.Single().EmployeeId=="e1")),Times.Once);
 }

 [Fact] public async Task Create_RejectsSelectedEmployeeMissingFromTenantScopeBeforePersistingRun()
 {
  var runs=Repo(Array.Empty<PayrollRun>());
  var employees=Repo(new[]{
   new EmpUser{CompanyId="c1",EmployeeId="e1",UserId="u1",FirstName="A",LastName="B",Email="a@x",Status=true,DateOfJoining="2026-01-01"},
   new EmpUser{CompanyId="c2",EmployeeId="foreign",UserId="u2",FirstName="C",LastName="D",Email="c@x",Status=true,DateOfJoining="2026-01-01"}
  });
  var service=new PayrollRunService(runs.Object,Repo(Array.Empty<PayrollRunEmployee>()).Object,employees.Object,Repo(Array.Empty<PayrollDivisorPolicy>()).Object);

  var result=await service.Create("c1","actor",new CreatePayrollRunRequestDto
  {
   PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,9,30),PayDate=new DateTime(2026,10,1),
   PopulationType="SELECTED",EmployeeIds=["e1","foreign","missing"]
  });

  Assert.False(result.Success); Assert.Equal(409,result.StatusCode);
  Assert.Contains("not found in this company",result.Message);
  runs.Verify(x=>x.AddOne(It.IsAny<PayrollRun>()),Times.Never);
 }

 [Fact] public async Task Create_RejectsNullSelectedEmployeeListWithoutThrowing()
 {
  var runs=Repo(Array.Empty<PayrollRun>());
  var service=new PayrollRunService(runs.Object,Repo(Array.Empty<PayrollRunEmployee>()).Object,Repo(Array.Empty<EmpUser>()).Object,Repo(Array.Empty<PayrollDivisorPolicy>()).Object);
  var result=await service.Create("c1","actor",new CreatePayrollRunRequestDto
  {
   PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,9,30),PayDate=new DateTime(2026,10,1),
   PopulationType="SELECTED",EmployeeIds=null!
  });
  Assert.False(result.Success); Assert.Equal(400,result.StatusCode);
  Assert.Contains("requires employees",result.Message);
  runs.Verify(x=>x.AddOne(It.IsAny<PayrollRun>()),Times.Never);
 }

 [Theory]
 [InlineData("e1","E1")]
 [InlineData("e1","")]
 public async Task Create_RejectsDuplicateOrBlankSelectedEmployeeIds(string firstId, string secondId)
 {
  var runs=Repo(Array.Empty<PayrollRun>());
  var service=new PayrollRunService(runs.Object,Repo(Array.Empty<PayrollRunEmployee>()).Object,Repo(Array.Empty<EmpUser>()).Object,Repo(Array.Empty<PayrollDivisorPolicy>()).Object);
  var result=await service.Create("c1","actor",new CreatePayrollRunRequestDto
  {
   PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,9,30),PayDate=new DateTime(2026,10,1),
   PopulationType="SELECTED",EmployeeIds=[firstId, secondId]
  });
  Assert.False(result.Success); Assert.Equal(400,result.StatusCode);
  runs.Verify(x=>x.AddOne(It.IsAny<PayrollRun>()),Times.Never);
 }
 [Fact] public async Task Create_ExcludesIneligibleEmployeesFromAllEligiblePopulation()
 {
  var runs=Repo(Array.Empty<PayrollRun>()); var lines=Repo(Array.Empty<PayrollRunEmployee>());
  var employees=Repo(new[]{
   new EmpUser{CompanyId="c1",EmployeeId="eligible",UserId="u1",FirstName="Eligible",LastName="Employee",Email="eligible@example.com",Status=true,DateOfJoining="2026-01-01"},
   new EmpUser{CompanyId="c1",EmployeeId="future",UserId="u2",FirstName="Future",LastName="Employee",Email="future@example.com",Status=true,DateOfJoining="2026-10-01"},
   new EmpUser{CompanyId="c1",EmployeeId="ended",UserId="u3",FirstName="Ended",LastName="Employee",Email="ended@example.com",Status=false,DateOfJoining="2024-01-01",LastWorkingDate=new DateTime(2026,8,31)},
   new EmpUser{CompanyId="c1",EmployeeId="missing-join-date",UserId="u4",FirstName="Missing",LastName="Date",Email="missing@example.com",Status=true}
  });
  var service=new PayrollRunService(runs.Object,lines.Object,employees.Object,Repo(Array.Empty<PayrollDivisorPolicy>()).Object);
  var result=await service.Create("c1","actor",new CreatePayrollRunRequestDto{PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,9,30),PayDate=new DateTime(2026,10,1)});
  Assert.True(result.Success); Assert.Equal(1,result.MethodResult.EmployeeCount);
  lines.Verify(x=>x.AddMany(It.Is<IEnumerable<PayrollRunEmployee>>(v=>v.Count()==1&&v.Single().EmployeeId=="eligible")),Times.Once);
 }
 [Fact] public async Task Create_RejectsSelectedIneligibleEmployeeBeforePersistingRun()
 {
  var runs=Repo(Array.Empty<PayrollRun>());
  var employees=Repo(new[]{new EmpUser{CompanyId="c1",EmployeeId="future",UserId="u1",FirstName="Future",LastName="Employee",Email="future@example.com",Status=true,DateOfJoining="2026-10-01"}});
  var service=new PayrollRunService(runs.Object,Repo(Array.Empty<PayrollRunEmployee>()).Object,employees.Object,Repo(Array.Empty<PayrollDivisorPolicy>()).Object);
  var result=await service.Create("c1","actor",new CreatePayrollRunRequestDto{PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,9,30),PayDate=new DateTime(2026,10,1),PopulationType="SELECTED",EmployeeIds=["future"]});
  Assert.False(result.Success); Assert.Equal(409,result.StatusCode);
  runs.Verify(x=>x.AddOne(It.IsAny<PayrollRun>()),Times.Never);
 }
 [Fact] public async Task Create_IsIdempotentForSameTenantKey()
 {
  var existing=new PayrollRun{Id="run-1",CompanyId="c1",PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,9,30),PayDate=new DateTime(2026,10,1),IdempotencyKey="same",CreatedAtUtc=DateTime.UtcNow};
  var runs=Repo(new[]{existing}); var lines=Repo(Array.Empty<PayrollRunEmployee>()); var employees=Repo(Array.Empty<EmpUser>()); var service=new PayrollRunService(runs.Object,lines.Object,employees.Object,Repo(Array.Empty<PayrollDivisorPolicy>()).Object);
  var result=await service.Create("c1","actor",new CreatePayrollRunRequestDto{PeriodStart=existing.PeriodStart,PeriodEnd=existing.PeriodEnd,PayDate=existing.PayDate,IdempotencyKey="same"});
  Assert.True(result.Success); Assert.Equal("run-1",result.ResourceId); runs.Verify(x=>x.AddOne(It.IsAny<PayrollRun>()),Times.Never);
 }
 [Fact] public async Task Create_RejectsReusedIdempotencyKeyForDifferentPayrollRequest()

 {
  var existing=new PayrollRun{Id="run-1",CompanyId="c1",PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,9,30),PayDate=new DateTime(2026,10,1),IdempotencyKey="same"};
  var runs=Repo(new[]{existing});
  var service=new PayrollRunService(runs.Object,Repo(Array.Empty<PayrollRunEmployee>()).Object,Repo(Array.Empty<EmpUser>()).Object,Repo(Array.Empty<PayrollDivisorPolicy>()).Object);
  var result=await service.Create("c1","actor",new CreatePayrollRunRequestDto
  {
   PeriodStart=existing.PeriodStart,PeriodEnd=existing.PeriodEnd,PayDate=new DateTime(2026,10,5),IdempotencyKey="same"
  });
  Assert.False(result.Success); Assert.Equal(409,result.StatusCode);
  Assert.Contains("different payroll request",result.Message);
  runs.Verify(x=>x.AddOne(It.IsAny<PayrollRun>()),Times.Never);
 }

 [Fact] public async Task Create_RequiresFreshIdempotencyKeyAfterRunWasCancelled()
 {
  var cancelled=new PayrollRun{Id="cancelled-run",CompanyId="c1",PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,9,30),PayDate=new DateTime(2026,10,1),Status="CANCELLED",IsActive=false,IdempotencyKey="old-key"};
  var runs=Repo(new[]{cancelled});
  var employees=Repo(new[]{new EmpUser{CompanyId="c1",EmployeeId="e1",UserId="u1",FirstName="A",LastName="B",Email="a@x",Status=true,DateOfJoining="2026-01-01"}});
  var service=new PayrollRunService(runs.Object,Repo(Array.Empty<PayrollRunEmployee>()).Object,employees.Object,Repo(Array.Empty<PayrollDivisorPolicy>()).Object);
  var request=new CreatePayrollRunRequestDto{PeriodStart=cancelled.PeriodStart,PeriodEnd=cancelled.PeriodEnd,PayDate=cancelled.PayDate,IdempotencyKey="old-key"};

  var staleKey=await service.Create("c1","actor",request);
  Assert.False(staleKey.Success); Assert.Equal(409,staleKey.StatusCode);
  request.IdempotencyKey="new-key";
  var replacement=await service.Create("c1","actor",request);

  Assert.True(replacement.Success); Assert.NotEqual(cancelled.Id,replacement.ResourceId);
  runs.Verify(x=>x.AddOne(It.Is<PayrollRun>(r=>r.IdempotencyKey=="new-key")),Times.Once);
 }
 [Fact] public async Task Create_NormalizesBlankIdempotencyKeyToNull()
 {
  var runs=Repo(Array.Empty<PayrollRun>());
  var employees=Repo(new[]{new EmpUser{CompanyId="c1",EmployeeId="e1",UserId="u1",FirstName="A",LastName="B",Email="a@x",Status=true,DateOfJoining="2026-01-01"}});
  var service=new PayrollRunService(runs.Object,Repo(Array.Empty<PayrollRunEmployee>()).Object,employees.Object,Repo(Array.Empty<PayrollDivisorPolicy>()).Object);
  var result=await service.Create("c1","actor",new CreatePayrollRunRequestDto{PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,9,30),PayDate=new DateTime(2026,10,1),IdempotencyKey="   "});
  Assert.True(result.Success);
  runs.Verify(x=>x.AddOne(It.Is<PayrollRun>(r=>r.IdempotencyKey==null)),Times.Once);
 }
 [Fact] public async Task Create_ReturnsConflictWhenEquivalentRunWinsConcurrentInsert()
 {
  var runs=Repo(Array.Empty<PayrollRun>());
  runs.Setup(x=>x.AddOne(It.IsAny<PayrollRun>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result{Success=false,StatusCode=500,Message="E11000 duplicate key index: ux_payrollrun_company_period_type_population"});
  var employees=Repo(new[]{new EmpUser{CompanyId="c1",EmployeeId="e1",UserId="u1",FirstName="A",LastName="B",Email="a@x",Status=true,DateOfJoining="2026-01-01"}});
  var service=new PayrollRunService(runs.Object,Repo(Array.Empty<PayrollRunEmployee>()).Object,employees.Object,Repo(Array.Empty<PayrollDivisorPolicy>()).Object);
  var result=await service.Create("c1","actor",new CreatePayrollRunRequestDto{PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,9,30),PayDate=new DateTime(2026,10,1)});
  Assert.False(result.Success); Assert.Equal(409,result.StatusCode); Assert.Contains("created concurrently",result.Message);
 }
 [Fact] public async Task Create_ReturnsConflictWhenActivePartialPeriodIndexRejectsConcurrentInsert()
 {
  var runs=Repo(Array.Empty<PayrollRun>());
  runs.Setup(x=>x.AddOne(It.IsAny<PayrollRun>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result{Success=false,StatusCode=500,Message="E11000 duplicate key index: ux_payrollrun_company_period_type_population_active"});
  var employees=Repo(new[]{new EmpUser{CompanyId="c1",EmployeeId="e1",UserId="u1",FirstName="A",LastName="B",Email="a@x",Status=true,DateOfJoining="2026-01-01"}});
  var service=new PayrollRunService(runs.Object,Repo(Array.Empty<PayrollRunEmployee>()).Object,employees.Object,Repo(Array.Empty<PayrollDivisorPolicy>()).Object);
  var result=await service.Create("c1","actor",new CreatePayrollRunRequestDto{PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,9,30),PayDate=new DateTime(2026,10,1)});
  Assert.False(result.Success); Assert.Equal(409,result.StatusCode); Assert.Contains("created concurrently",result.Message);
 }
 [Fact] public async Task Create_ReturnsWinningRunForConcurrentMatchingIdempotencyKey()
 {
  var winner=new PayrollRun{Id="winner",CompanyId="c1",PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,9,30),PayDate=new DateTime(2026,10,1),IdempotencyKey="same-key"};
  var firstRead=0; var runs=new Mock<IMongoDbRepository<PayrollRun>>();
  runs.Setup(x=>x.FirstOrDefault(It.IsAny<Expression<Func<PayrollRun,bool>>>(),It.IsAny<bool>(),It.IsAny<bool>()))
   .Returns((Expression<Func<PayrollRun,bool>> _,bool _,bool _)=>Task.FromResult(Interlocked.Increment(ref firstRead)==1?(PayrollRun?)null:winner));
  runs.Setup(x=>x.Exist(It.IsAny<Expression<Func<PayrollRun,bool>>>(),It.IsAny<bool>())).ReturnsAsync(false);
  runs.Setup(x=>x.AddOne(It.IsAny<PayrollRun>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result{Success=false,StatusCode=500,Message="E11000 duplicate key index: ux_payrollrun_company_idempotency"});
  var employees=Repo(new[]{new EmpUser{CompanyId="c1",EmployeeId="e1",UserId="u1",FirstName="A",LastName="B",Email="a@x",Status=true,DateOfJoining="2026-01-01"}});
  var service=new PayrollRunService(runs.Object,Repo(Array.Empty<PayrollRunEmployee>()).Object,employees.Object,Repo(Array.Empty<PayrollDivisorPolicy>()).Object);
  var result=await service.Create("c1","actor",new CreatePayrollRunRequestDto{PeriodStart=winner.PeriodStart,PeriodEnd=winner.PeriodEnd,PayDate=winner.PayDate,IdempotencyKey="same-key"});
  Assert.True(result.Success); Assert.Equal("winner",result.ResourceId);
 }
 [Fact] public async Task Create_RejectsSelectedPopulationWithoutEmployees()
 {
  var service=new PayrollRunService(Repo(Array.Empty<PayrollRun>()).Object,Repo(Array.Empty<PayrollRunEmployee>()).Object,Repo(Array.Empty<EmpUser>()).Object,Repo(Array.Empty<PayrollDivisorPolicy>()).Object);
  var result=await service.Create("c1","actor",new CreatePayrollRunRequestDto{PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,9,30),PayDate=new DateTime(2026,10,1),PopulationType="SELECTED"});
  Assert.False(result.Success); Assert.Equal(400,result.StatusCode);
 }

 [Fact] public async Task Create_RejectsMidPeriodPolicyChangeBeforePersistingRun()
 {
  var runs=Repo(Array.Empty<PayrollRun>());
  var employees=Repo(new[]{new EmpUser{CompanyId="c1",EmployeeId="e1",UserId="u1",FirstName="A",LastName="B",Email="a@x",DateOfJoining="2026-01-01",Status=true}});
  var salaries=Repo(new[]{
   new EmployeeSalaryStructure{Id="salary-1",CompanyId="c1",EmployeeId="e1",PayGroupId="g1",EffectiveFrom=new DateTime(2026,9,1),EffectiveTo=new DateTime(2026,9,14),Status="ACTIVE",ApprovalStatus="APPROVED"},
   new EmployeeSalaryStructure{Id="salary-2",CompanyId="c1",EmployeeId="e1",PayGroupId="g2",EffectiveFrom=new DateTime(2026,9,15),Status="ACTIVE",ApprovalStatus="APPROVED"}
  });
  var groups=Repo(new[]{
   new PayrollPayGroup{Id="g1",CompanyId="c1",Name="Group 1",PolicyProfileId="p1",EffectiveFrom=new DateTime(2026,1,1)},
   new PayrollPayGroup{Id="g2",CompanyId="c1",Name="Group 2",PolicyProfileId="p2",EffectiveFrom=new DateTime(2026,1,1)}
  });
  var profiles=Repo(new[]{
   new PayrollPolicyProfile{Id="p1",CompanyId="c1",Name="Policy 1",EffectiveFrom=new DateTime(2026,1,1),ApprovalStatus="APPROVED"},
   new PayrollPolicyProfile{Id="p2",CompanyId="c1",Name="Policy 2",EffectiveFrom=new DateTime(2026,1,1),ApprovalStatus="APPROVED"}
  });
  var service=new PayrollRunService(runs.Object,Repo(Array.Empty<PayrollRunEmployee>()).Object,employees.Object,
   Repo(Array.Empty<PayrollDivisorPolicy>()).Object,payGroups:groups.Object,policyProfiles:profiles.Object,employeeSalaryStructures:salaries.Object);

  var result=await service.Create("c1","actor",new CreatePayrollRunRequestDto{PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,9,30),PayDate=new DateTime(2026,10,1)});

  Assert.False(result.Success); Assert.Equal(409,result.StatusCode); Assert.Contains("2026-09-15",result.Message);
  runs.Verify(x=>x.AddOne(It.IsAny<PayrollRun>()),Times.Never);
 }

 [Theory]
 [InlineData(true)]
 [InlineData(false)]
 public async Task Create_CancelsAndAuditsRunWhenEmployeeLinePersistenceFails(bool cleanupSucceeds)
 {
  var runs=Repo(Array.Empty<PayrollRun>());
  PayrollRun? cancelledRun=null;
  runs.Setup(x=>x.Update(It.IsAny<FilterDefinition<PayrollRun>>(),It.IsAny<PayrollRun>()))
   .Callback<FilterDefinition<PayrollRun>,PayrollRun>((_,run)=>cancelledRun=run)
   .ReturnsAsync(new Codeji.CMS.Domain.Models.Result{Success=true});
  var lineRepository=Repo(Array.Empty<PayrollRunEmployee>());
  lineRepository.Setup(x=>x.AddMany(It.IsAny<IEnumerable<PayrollRunEmployee>>()))
   .ReturnsAsync(new Codeji.CMS.Domain.Models.Result{Success=false,StatusCode=500,Message="insert failed"});
  lineRepository.Setup(x=>x.DeleteAll(It.IsAny<FilterDefinition<PayrollRunEmployee>>()))
   .ReturnsAsync(new Codeji.CMS.Domain.Models.Result{Success=cleanupSucceeds,StatusCode=cleanupSucceeds?200:500});
  var employees=Repo(new[]{new EmpUser{CompanyId="c1",EmployeeId="e1",UserId="u1",FirstName="A",LastName="B",Email="a@x",Status=true,DateOfJoining="2026-01-01"}});
  var service=new PayrollRunService(runs.Object,lineRepository.Object,employees.Object,Repo(Array.Empty<PayrollDivisorPolicy>()).Object);

  var result=await service.Create("c1","creator",new CreatePayrollRunRequestDto
  {
   PeriodStart=new DateTime(2026,9,1),PeriodEnd=new DateTime(2026,9,30),PayDate=new DateTime(2026,10,1),IdempotencyKey="create-key"
  });

  Assert.False(result.Success);
  Assert.Equal(500,result.StatusCode);
  Assert.Contains("new-run",result.Message);
  Assert.NotNull(cancelledRun);
  Assert.Equal("CANCELLED",cancelledRun!.Status);
  Assert.False(cancelledRun.IsActive);
  Assert.Equal("creator",Assert.Single(cancelledRun.AuditHistory).ActorId);
  Assert.Equal("RunCreationFailed",Assert.Single(cancelledRun.AuditHistory).Action);
  Assert.Contains(cleanupSucceeds?"removed":"partial lines may require cleanup",Assert.Single(cancelledRun.AuditHistory).Notes!,StringComparison.OrdinalIgnoreCase);
  lineRepository.Verify(x=>x.DeleteAll(It.IsAny<FilterDefinition<PayrollRunEmployee>>()),Times.Once);

  }
 static Mock<IMongoDbRepository<T>> Repo<T>(IEnumerable<T> items)
 { var a=items.ToArray(); var m=new Mock<IMongoDbRepository<T>>(); m.Setup(x=>x.Exist(It.IsAny<Expression<Func<T,bool>>>(),It.IsAny<bool>())).Returns((Expression<Func<T,bool>> p,bool _)=>Task.FromResult(a.Any(p.Compile()))); m.Setup(x=>x.FirstOrDefault(It.IsAny<Expression<Func<T,bool>>>(),It.IsAny<bool>(),It.IsAny<bool>())).Returns((Expression<Func<T,bool>> p,bool _,bool _)=>Task.FromResult(a.FirstOrDefault(p.Compile()))); m.Setup(x=>x.GetAll(It.IsAny<Expression<Func<T,bool>>>(),It.IsAny<bool>(),It.IsAny<bool>())).Returns((Expression<Func<T,bool>> p,bool _,bool _)=>Task.FromResult<IEnumerable<T>>(a.Where(p.Compile()).ToArray())); m.Setup(x=>x.AddOne(It.IsAny<T>())).Callback<T>(x=>{if(x is PayrollRun r&&string.IsNullOrEmpty(r.Id))r.Id="new-run";}).ReturnsAsync(new Codeji.CMS.Domain.Models.Result{Success=true}); m.Setup(x=>x.AddMany(It.IsAny<IEnumerable<T>>())).ReturnsAsync(new Codeji.CMS.Domain.Models.Result{Success=true}); return m; }
}
