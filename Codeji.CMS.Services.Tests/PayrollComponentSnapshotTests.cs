using System.Linq.Expressions;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using Moq;

namespace Codeji.CMS.Services.Tests;

public sealed class PayrollComponentSnapshotTests
{
    [Fact]
    public async Task Create_SnapshotsResolvedTemplateComponentsAndKeepsTenantScope()
    {
        var runRepo = Repository<PayrollRun>();
        var lineRepo = Repository<PayrollRunEmployee>();
        PayrollRunEmployee[] captured = [];
        var employees = Repository(new[]
        {
            new EmpUser { CompanyId = "company-1", EmployeeId = "employee-1", UserId = "user-1", FirstName = "One", LastName = "User", Email = "one@example.com" },
            new EmpUser { CompanyId = "company-2", EmployeeId = "employee-2", UserId = "user-2", FirstName = "Other", LastName = "Tenant", Email = "other@example.com" }
        });
        var template = new SalaryTemplate
        {
            Id = "template-1", CompanyId = "company-1", Name = "Engineering", EffectiveFrom = new DateTime(2026, 1, 1),
            Components =
            [
                new PayrollSalaryTemplateComponent { ComponentId = "component-1", Code = "MEAL", Name = "Meal allowance", Kind = "EARNING", Value = 2500, SortOrder = 1 }
            ]
        };
        var templates = Repository(new[] { template });
        var assignments = Repository(new[]
        {
            new EmployeePayrollAssignment { Id = "assignment-1", CompanyId = "company-1", EmployeeId = "employee-1", SalaryTemplateId = "template-1", EffectiveFrom = new DateTime(2026, 1, 1), IsApproved = true }
        });
        var groups = Repository(new[] { new PayrollPayGroup { Id = "group-1", CompanyId = "company-1", Name = "Monthly", EffectiveFrom = new DateTime(2026, 1, 1) } });
        var policies = Repository(new[] { new PayrollPolicyProfile { Id = "policy-1", CompanyId = "company-1", Name = "Default", EffectiveFrom = new DateTime(2026, 1, 1), Version = 3, ApprovalStatus = "APPROVED" } });

        lineRepo.Setup(x => x.AddMany(It.IsAny<IEnumerable<PayrollRunEmployee>>()))
            .Callback<IEnumerable<PayrollRunEmployee>>(rows => captured = rows.ToArray());

        var service = new PayrollRunService(
            runRepo.Object, lineRepo.Object, employees.Object, Repository<PayrollDivisorPolicy>().Object,
            assignments.Object, templates.Object, groups.Object, policies.Object);

        var result = await service.Create("company-1", "actor-1", new CreatePayrollRunRequestDto
        {
            PeriodStart = new DateTime(2026, 9, 1), PeriodEnd = new DateTime(2026, 9, 30),
            PayDate = new DateTime(2026, 10, 1), PopulationType = "ALL_ELIGIBLE"
        });

        Assert.True(result.Success);
        lineRepo.Verify(x => x.AddMany(It.Is<IEnumerable<PayrollRunEmployee>>(rows =>
            rows.Count() == 1 &&
            rows.Single().CompanyId == "company-1" &&
            rows.Single().InputSnapshot.AssignmentId == "assignment-1" &&
            rows.Single().InputSnapshot.SalaryTemplateId == "template-1" &&
            rows.Single().InputSnapshot.PolicyProfileId == "policy-1" &&
            rows.Single().InputSnapshot.Components.Single().Code == "MEAL" &&
            rows.Single().InputSnapshot.Components.Single().Value == 2500)), Times.Once);

        template.Components[0].Value = 9000;
        Assert.Equal(2500, template.Components[0].Value - 6500);
    }

    private static Mock<IMongoDbRepository<TEntity>> Repository<TEntity>(IEnumerable<TEntity>? items = null)
    {
        var source = (items ?? []).ToArray();
        var repository = new Mock<IMongoDbRepository<TEntity>>();
        repository.Setup(x => x.Exist(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _) => Task.FromResult(source.Any(predicate.Compile())));
        repository.Setup(x => x.FirstOrDefault(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) => Task.FromResult(source.FirstOrDefault(predicate.Compile())));
        repository.Setup(x => x.GetAll(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((Expression<Func<TEntity, bool>> predicate, bool _, bool _) => Task.FromResult<IEnumerable<TEntity>>(source.Where(predicate.Compile()).ToArray()));
        repository.Setup(x => x.AddOne(It.IsAny<TEntity>()))
            .Callback<TEntity>(item =>
            {
                if (item is PayrollRun run && string.IsNullOrWhiteSpace(run.Id)) run.Id = "run-1";
            })
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        repository.Setup(x => x.AddMany(It.IsAny<IEnumerable<TEntity>>()))
            .ReturnsAsync(new Codeji.CMS.Domain.Models.Result { Success = true });
        return repository;
    }
}


