using System.Security.Claims;
using Codeji.CMS.API.Controllers;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.PayRoll;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Tests;

public sealed class PayrollAdjustmentControllerTests
{
    [Theory]
    [InlineData(201)]
    [InlineData(400)]
    [InlineData(409)]
    public async Task Create_UsesServiceStatusCodeForHttpResponse(int serviceStatusCode)
    {
        var service = new StubPayrollAdjustmentService { CreateStatusCode = serviceStatusCode };
        var controller = CreateController(service);

        var response = await controller.Create(new CreatePayrollAdjustmentRequestDto
        {
            OriginalPayrollRunId = "run-1", EmployeeId = "employee-1", ComponentCode = "BONUS",
            TargetPeriod = new DateTime(2026, 9, 1), Amount = 10, Reason = "Approved correction"
        });

        Assert.Equal(serviceStatusCode, Assert.IsType<ObjectResult>(response).StatusCode);
    }

    [Fact]
    public async Task Process_ReturnsConflictHttpStatusWhenAmountTreatmentIsNotImplemented()
    {
        var controller = CreateController(new StubPayrollAdjustmentService());

        var response = await controller.Process("adjustment-1", new PayrollAdjustmentActionDto());

        Assert.Equal(StatusCodes.Status409Conflict, Assert.IsType<ObjectResult>(response).StatusCode);
    }

    [Fact]
    public async Task Reject_UsesServiceStatusCodeForHttpResponse()
    {
        var service = new StubPayrollAdjustmentService { RejectStatusCode = StatusCodes.Status409Conflict };
        var response = await CreateController(service).Reject("adjustment-1", new RejectPayrollAdjustmentRequestDto { Reason = "Duplicate request" });
        Assert.Equal(StatusCodes.Status409Conflict, Assert.IsType<ObjectResult>(response).StatusCode);
    }

    private static PayrollAdjustmentController CreateController(IPayrollAdjustmentService service)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("CompanyId", "company-1"),
            new Claim("UserId", "actor-1")
        ], "test"));
        return new PayrollAdjustmentController(service, new HttpContextAccessor { HttpContext = context });
    }

    private sealed class StubPayrollAdjustmentService : IPayrollAdjustmentService
    {
        public int CreateStatusCode { get; init; } = StatusCodes.Status201Created;
        public int RejectStatusCode { get; init; } = StatusCodes.Status200OK;

        public Task<Result<List<PayrollAdjustmentListItemDto>>> GetAll(string companyId) =>
            Task.FromResult(new Result<List<PayrollAdjustmentListItemDto>>());

        public Task<Result<List<PayrollSalaryComponentDto>>> GetComponents(string companyId, DateTime effectiveDate) =>
            Task.FromResult(new Result<List<PayrollSalaryComponentDto>> { Success = true, MethodResult = [] });

        public Task<Result<PayrollAdjustment>> Create(string companyId, string actor, CreatePayrollAdjustmentRequestDto request) =>
            Task.FromResult(new Result<PayrollAdjustment> { Success = CreateStatusCode < 400, StatusCode = CreateStatusCode });

        public Task<Result> Approve(string companyId, string actor, string id) =>
            Task.FromResult(new Result { Success = false, StatusCode = StatusCodes.Status403Forbidden });

        public Task<Result> Reject(string companyId, string actor, string id, RejectPayrollAdjustmentRequestDto request) =>
            Task.FromResult(new Result { Success = RejectStatusCode < 400, StatusCode = RejectStatusCode });

        public Task<Result> Process(string companyId, string actor, string id, PayrollAdjustmentActionDto request) =>
            Task.FromResult(new Result { Success = false, StatusCode = StatusCodes.Status409Conflict });
    }
}
