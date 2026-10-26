using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Attendance;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.LeaveManagement;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Text.Json;
using Moq;
using System.Security.Claims;

namespace Codeji.CMS.Services.Tests;

public class WorkFromHomeModuleAccessibilityTests
{
    [Fact]
    public void IdempotencyKey_IsBoundToTheAuthenticatedCompanyAndUser()
    {
        var predicate = WorkFromHomeService.BuildIdempotentRequestPredicate("company-a", "user-a", "a-valid-retry-key-1234").Compile();
        var request = new WorkFromHomeRequest { CompanyId = "company-a", UserId = "user-a", IdempotencyKey = "a-valid-retry-key-1234" };

        Assert.True(predicate(request));
        Assert.False(predicate(new WorkFromHomeRequest { CompanyId = "company-b", UserId = "user-a", IdempotencyKey = request.IdempotencyKey }));
        Assert.False(predicate(new WorkFromHomeRequest { CompanyId = "company-a", UserId = "user-b", IdempotencyKey = request.IdempotencyKey }));
        Assert.False(predicate(new WorkFromHomeRequest { CompanyId = "company-a", UserId = "user-a", IdempotencyKey = "another-retry-key-1234" }));
    }

    [Fact]
    public void RequestPayloadHash_NormalizesWhitespaceAndReasonCodeCase()
    {
        var first = new WorkFromHomeRequestDto
        {
            FromDate = new DateOnly(2026, 10, 26), ToDate = new DateOnly(2026, 10, 26),
            DurationType = " FullDay ", ReasonCode = " personal ", ReasonText = " doctor appointment "
        };
        var retry = new WorkFromHomeRequestDto
        {
            FromDate = new DateOnly(2026, 10, 26), ToDate = new DateOnly(2026, 10, 26),
            DurationType = "FullDay", ReasonCode = "PERSONAL", ReasonText = "doctor appointment"
        };

        Assert.Equal(WorkFromHomeService.ComputeRequestPayloadHash(first), WorkFromHomeService.ComputeRequestPayloadHash(retry));
        var changedRequest = new WorkFromHomeRequestDto
        {
            FromDate = retry.FromDate, ToDate = new DateOnly(2026, 10, 27), DurationType = retry.DurationType,
            ReasonCode = retry.ReasonCode, ReasonText = retry.ReasonText
        };
        Assert.NotEqual(WorkFromHomeService.ComputeRequestPayloadHash(first), WorkFromHomeService.ComputeRequestPayloadHash(changedRequest));
    }

    [Fact]
    public void IdempotencyKey_RejectsOversizedAndControlCharacterValues()
    {
        Assert.Null(WorkFromHomeService.NormalizeIdempotencyKey("too-short"));
        Assert.Null(WorkFromHomeService.NormalizeIdempotencyKey(new string('x', 129)));
        Assert.Null(WorkFromHomeService.NormalizeIdempotencyKey("valid-retry-key-123\n"));
        Assert.Equal("valid-retry-key-123", WorkFromHomeService.NormalizeIdempotencyKey(" valid-retry-key-123 "));
    }

    [Theory]
    [InlineData(" personal ", "PERSONAL")]
    [InlineData("Medical", "MEDICAL")]
    [InlineData("OTHER", "OTHER")]
    [InlineData("", null)]
    [InlineData("unknown-category", null)]
    [InlineData("<script>", null)]
    public void ReasonCode_AcceptsOnlySupportedCategories(string? input, string? expected)
    {
        Assert.Equal(expected, WorkFromHomeService.NormalizeReasonCode(input));
    }

    [Theory]
    [InlineData(null, "WFH_REASON_CODE_REQUIRED")]
    [InlineData("", "WFH_REASON_CODE_REQUIRED")]
    [InlineData("<script>", "WFH_INVALID_REASON_CODE")]
    public async Task Create_WithMissingOrUnsupportedReasonCode_FailsBeforeIdempotencyLookup(string? reasonCode, string expectedCode)
    {
        var dto = new WorkFromHomeRequestDto
        {
            FromDate = new DateOnly(2026, 10, 26), ToDate = new DateOnly(2026, 10, 26),
            DurationType = "FullDay", ReasonCode = reasonCode!, ReasonText = "appointment"
        };

        var result = await CreateService(isModuleAccessible: true).Create(dto, "retry-key-for-invalid-reason");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.StartsWith($"{expectedCode}:", result.Message);
        _requestRepo.Verify(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<WorkFromHomeRequest, bool>>>()), Times.Never);
        _policyRepo.Verify(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<WorkFromHomePolicy, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithoutIdempotencyKey_FailsBeforeReadingPolicyOrCreatingRequest()
    {
        var dto = new WorkFromHomeRequestDto
        {
            FromDate = new DateOnly(2026, 10, 26), ToDate = new DateOnly(2026, 10, 26),
            DurationType = "FullDay", ReasonCode = "PERSONAL", ReasonText = "appointment"
        };

        var result = await CreateService(isModuleAccessible: true).Create(dto, null!);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.StartsWith("WFH_IDEMPOTENCY_KEY_REQUIRED:", result.Message);
        _policyRepo.Verify(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<WorkFromHomePolicy, bool>>>()), Times.Never);
        _requestRepo.Verify(x => x.GetAll(It.IsAny<System.Linq.Expressions.Expression<Func<WorkFromHomeRequest, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void RequestDto_ParsesBusinessDatesWithoutTimezoneConversion()
    {
        var dto = JsonSerializer.Deserialize<WorkFromHomeRequestDto>("""{"fromDate":"2026-10-26","toDate":"2026-10-27"}""", new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(dto);
        Assert.Equal(new DateOnly(2026, 10, 26), dto.FromDate);
        Assert.Equal(new DateOnly(2026, 10, 27), dto.ToDate);
    }

    [Fact]
    public void RequestDto_RejectsTimestampInsteadOfDateOnly()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<WorkFromHomeRequestDto>(
            """{"fromDate":"2026-10-26T00:00:00Z","toDate":"2026-10-27T00:00:00Z"}""", new JsonSerializerOptions { PropertyNameCaseInsensitive = true }));
    }

    [Fact]
    public void RequestDto_RequiresBothBusinessDates()
    {
        var dto = new WorkFromHomeRequestDto { FromDate = new DateOnly(2026, 10, 26) };

        Assert.False(WorkFromHomeService.HasRequiredBusinessDates(dto));
        Assert.True(WorkFromHomeService.HasRequiredBusinessDates(new WorkFromHomeRequestDto
        {
            FromDate = new DateOnly(2026, 10, 26), ToDate = new DateOnly(2026, 10, 26)
        }));
    }

    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<IMongoDbRepository<WorkFromHomePolicy>> _policyRepo = new();
    private readonly Mock<IMongoDbRepository<WorkFromHomeRequest>> _requestRepo = new();
    private readonly Mock<IMongoDbRepository<WorkFromHomeOutboxEvent>> _outboxRepo = new();
    private readonly Mock<IMongoDbRepository<AttendancePayrollException>> _exceptionRepo = new();
    private readonly Mock<IMongoDbRepository<LeaveRequest>> _leaveRequestRepo = new();
    private readonly Mock<IMongoDbRepository<EmpUser>> _employeeRepo = new();
    private readonly Mock<IMongoClient> _mongoClient = new();
    private readonly Mock<ILeaveManagementService> _leaveManagement = new();
    private readonly Mock<ILeaveAttendanceReconciliationService> _leaveAttendanceReconciliation = new();

    private static BsonDocument Render<T>(FilterDefinition<T> filter) =>
        filter.Render(new RenderArgs<T>(BsonSerializer.SerializerRegistry.GetSerializer<T>(), BsonSerializer.SerializerRegistry));

    private readonly Mock<IRoleService> _roles = new();

    private WorkFromHomeService CreateService(bool isModuleAccessible, string companyId = "comp-123", string userId = "user-456")
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(Codeji.CMS.Utility.Enums.EnumsHelper.ClaimTypesEnum.company_id.ToString(), companyId),
            new Claim(Codeji.CMS.Utility.Enums.EnumsHelper.ClaimTypesEnum.user_id.ToString(), userId),
            new Claim(ClaimTypes.Role, "Administrator")
        ], "TestAuth"));
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        _roles.Setup(x => x.IsModuleAccessibleForCompany("Work_From_Home", companyId))
            .ReturnsAsync(isModuleAccessible);
        _roles.Setup(x => x.VerifyUserAccess(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserCheckModel>()))
            .ReturnsAsync(true);
        _leaveManagement.Setup(x => x.WithdrawAcceptedLeaveForWfhConflictAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<IClientSessionHandle, LeaveRequest, Task<Result>>>() ))
            .ReturnsAsync(new Result { Success = false, StatusCode = 503, Message = "LEAVE_WFH_CONFLICT_TRANSACTION_REQUIRED: test transaction unavailable" });
        _leaveManagement.Setup(x => x.ExecuteAcceptedLeaveWfhConflictTransactionAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<IClientSessionHandle, LeaveRequest, Task<Result>>>() ))
            .ReturnsAsync(new Result { Success = false, StatusCode = 503, Message = "LEAVE_WFH_CONFLICT_TRANSACTION_REQUIRED: test transaction unavailable" });
        _outboxRepo.Setup(x => x.AddOne(It.IsAny<WorkFromHomeOutboxEvent>())).ReturnsAsync(new Result { Success = true });

        return new WorkFromHomeService(
            _httpContextAccessor.Object,
            _policyRepo.Object,
            _requestRepo.Object,
            _outboxRepo.Object,
            _leaveRequestRepo.Object,
            _employeeRepo.Object,
            Mock.Of<IMongoDbRepository<Department>>(),
            Mock.Of<IMongoDbRepository<JobTitles>>(),
            Mock.Of<IMongoDbRepository<AttendanceStatusSetting>>(),
            Mock.Of<IMongoDbRepository<AttendanceModel>>(),
            Mock.Of<IMongoDbRepository<AttendanceDaySegment>>(),
            _exceptionRepo.Object,
            Mock.Of<IMongoDbRepository<MonthlyAttendanceSummary>>(),
            Mock.Of<IMongoDbRepository<WeeklyOffSetting>>(),
            Mock.Of<IMongoDbRepository<CalendarEntity>>(),
            Mock.Of<IMongoDbRepository<AttendanceRemarkOption>>(),
            Mock.Of<IAttendanceEditGuard>(),
            Mock.Of<IEffectiveOfficeScheduleService>(),
            Mock.Of<IAttendanceInitializationService>(),
            _roles.Object,
            Mock.Of<IMongoDbRepository<Roles>>(),
            _mongoClient.Object,
            _leaveManagement.Object,
            _leaveAttendanceReconciliation.Object,
            new AttendanceStatusCalculationService()
        );
    }

    [Fact]
    public async Task Create_WhenSameKeyAndPayloadAreRetried_ReturnsExistingTenantScopedRequest()
    {
        const string key = "retry-key-for-wfh-request-1";
        var dto = new WorkFromHomeRequestDto
        {
            FromDate = new DateOnly(2026, 10, 26), ToDate = new DateOnly(2026, 10, 26),
            DurationType = "FullDay", ReasonCode = "PERSONAL", ReasonText = "appointment"
        };
        var existing = new WorkFromHomeRequest
        {
            CompanyId = "comp-123", UserId = "user-456", RequestId = "request-existing", IdempotencyKey = key,
            IdempotencyPayloadHash = WorkFromHomeService.ComputeRequestPayloadHash(dto),
            FromDate = new DateTime(2026, 10, 26), ToDate = new DateTime(2026, 10, 26), Status = "Pending"
        };
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<WorkFromHomeRequest, bool>>>()))
            .ReturnsAsync(existing);

        var result = await CreateService(isModuleAccessible: true).Create(dto, key);

        Assert.True(result.Success);
        Assert.Equal("request-existing", result.MethodResult.RequestId);
        _policyRepo.Verify(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<WorkFromHomePolicy, bool>>>()), Times.Never);
        _requestRepo.Verify(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<WorkFromHomeRequest, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenKeyIsReusedForDifferentPayload_ReturnsConflictWithoutCreating()
    {
        const string key = "retry-key-for-wfh-request-2";
        var dto = new WorkFromHomeRequestDto
        {
            FromDate = new DateOnly(2026, 10, 26), ToDate = new DateOnly(2026, 10, 26),
            DurationType = "FullDay", ReasonCode = "PERSONAL", ReasonText = "appointment"
        };
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<WorkFromHomeRequest, bool>>>()))
            .ReturnsAsync(new WorkFromHomeRequest
            {
                CompanyId = "comp-123", UserId = "user-456", RequestId = "request-existing", IdempotencyKey = key,
                IdempotencyPayloadHash = "different-payload-hash"
            });

        var result = await CreateService(isModuleAccessible: true).Create(dto, key);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.StartsWith("WFH_IDEMPOTENCY_KEY_REUSED:", result.Message);
        _policyRepo.Verify(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<WorkFromHomePolicy, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenWfhModuleIsDisabled_DoesNotReturnAnIdempotentReplay()
    {
        const string key = "retry-key-for-disabled-module";
        var dto = new WorkFromHomeRequestDto
        {
            FromDate = new DateOnly(2026, 10, 26), ToDate = new DateOnly(2026, 10, 26),
            DurationType = "FullDay", ReasonCode = "PERSONAL", ReasonText = "appointment"
        };
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<WorkFromHomeRequest, bool>>>()))
            .ReturnsAsync(new WorkFromHomeRequest
            {
                CompanyId = "comp-123", UserId = "user-456", RequestId = "private-request", IdempotencyKey = key,
                IdempotencyPayloadHash = WorkFromHomeService.ComputeRequestPayloadHash(dto)
            });

        var result = await CreateService(isModuleAccessible: false).Create(dto, key);

        Assert.False(result.Success);
        Assert.StartsWith("WFH_MODULE_DISABLED:", result.Message);
        _requestRepo.Verify(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<WorkFromHomeRequest, bool>>>()), Times.Never);
    }

    [Theory]
    [InlineData("KEEP_LEAVE_CANCEL_WFH")]
    [InlineData("KEEP_WFH_CANCEL_LEAVE")]
    public async Task ResolveAttendanceConflict_WhenBothSourceIdsAreNotAvailable_FailsClosed(string resolution)
    {
        const string exceptionId = "exception-1";
        _exceptionRepo.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<AttendancePayrollException, bool>>>() ))
            .ReturnsAsync(new AttendancePayrollException
            {
                Id = exceptionId, CompanyId = "comp-123", UserId = "user-456", EmployeeId = "employee-1",
                PayrollMonth = new DateTime(2026, 10, 1), ExceptionType = "WFH_LEAVE_CONFLICT",
                Status = "PENDING_REVIEW", Version = 2, LeaveRequestId = "leave-request-1"
            });

        var result = await CreateService(isModuleAccessible: true).ResolveAttendanceConflict(exceptionId, new WorkFromHomeConflictResolutionDto
        {
            Resolution = resolution, Remarks = "Reviewed source ownership", ExceptionVersion = 2
        });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.StartsWith("WFH_CONFLICT_SOURCE_LINK_UNAVAILABLE:", result.Message);
        _requestRepo.Verify(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<WorkFromHomeRequest, bool>>>()), Times.Never);
        _requestRepo.Verify(x => x.Update(It.IsAny<FilterDefinition<WorkFromHomeRequest>>(), It.IsAny<WorkFromHomeRequest>()), Times.Never);
        _exceptionRepo.Verify(x => x.Update(It.IsAny<FilterDefinition<AttendancePayrollException>>(), It.IsAny<AttendancePayrollException>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAttendanceConflict_WithNullPayloadReturnsValidationErrorWithoutReadingException()
    {
        var result = await CreateService(isModuleAccessible: true).ResolveAttendanceConflict("exception-1", null!);

        Assert.False(result.Success);
        Assert.StartsWith("WFH_CONFLICT_DECISION_INVALID:", result.Message);
        _exceptionRepo.Verify(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<AttendancePayrollException, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAttendanceConflict_WhenKeepLeaveTransactionIsUnavailable_FailsClosed()
    {
        _exceptionRepo.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<AttendancePayrollException, bool>>>() ))
            .ReturnsAsync(new AttendancePayrollException
            {
                Id = "exception-linked", CompanyId = "comp-123", UserId = "user-456", EmployeeId = "employee-1",
                PayrollMonth = new DateTime(2026, 10, 1), ExceptionType = "LEAVE_ATTENDANCE_CONFLICT",
                Status = "PENDING_REVIEW", Version = 3, LeaveRequestId = "leave-request-1", WorkFromHomeRequestId = "wfh-request-1"
            });
        _leaveRequestRepo.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<LeaveRequest, bool>>>() ))
            .ReturnsAsync(new LeaveRequest
            {
                CompanyId = "comp-123", LeaveRequestId = "leave-request-1", EmployeeId = "user-456",
                LeavePolicyId = "leave-policy-1", Reason = "Test leave",
                Status = Codeji.CMS.Utility.Enums.EnumsHelper.LeaveRequestStatus.Accepted, Version = 1
            });

        var result = await CreateService(isModuleAccessible: true).ResolveAttendanceConflict("exception-linked", new WorkFromHomeConflictResolutionDto
        {
            Resolution = "KEEP_LEAVE_CANCEL_WFH", Remarks = "Reviewed both linked requests", ExceptionVersion = 3
        });

        Assert.False(result.Success);
        Assert.Equal(503, result.StatusCode);
        Assert.StartsWith("LEAVE_WFH_CONFLICT_TRANSACTION_REQUIRED:", result.Message);
        _leaveManagement.Verify(x => x.ExecuteAcceptedLeaveWfhConflictTransactionAsync("leave-request-1", "Reviewed both linked requests", It.IsAny<Func<IClientSessionHandle, LeaveRequest, Task<Result>>>()), Times.Once);
        _requestRepo.Verify(x => x.Update(It.IsAny<FilterDefinition<WorkFromHomeRequest>>(), It.IsAny<WorkFromHomeRequest>()), Times.Never);
        _exceptionRepo.Verify(x => x.Update(It.IsAny<FilterDefinition<AttendancePayrollException>>(), It.IsAny<AttendancePayrollException>()), Times.Never);
    }

    [Fact]
    public async Task GetPolicy_WhenModuleDisabled_ReturnsDisabledPolicyDto()
    {
        var service = CreateService(isModuleAccessible: false);

        var result = await service.GetPolicy();

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.False(result.MethodResult.IsEnabled);
        Assert.Equal(0, result.MethodResult.MaxDaysPerWeek);
    }

    [Fact]
    public async Task SavePolicy_WhenAssignmentListIsNull_ReturnsValidationFailureWithoutWriting()
    {
        var service = CreateService(isModuleAccessible: true);

        var result = await service.SavePolicy(new WorkFromHomePolicyDto
        {
            ApplicableDepartments = null!
        });

        Assert.False(result.Success);
        Assert.StartsWith("WFH_INVALID_POLICY:", result.Message);
        _policyRepo.Verify(x => x.AddOne(It.IsAny<WorkFromHomePolicy>()), Times.Never);
        _policyRepo.Verify(x => x.Update(It.IsAny<FilterDefinition<WorkFromHomePolicy>>(), It.IsAny<WorkFromHomePolicy>()), Times.Never);
    }

    [Fact]
    public async Task SavePolicy_WhenSelfApprovalIsEnabled_FailsClosedWithoutWriting()
    {
        var service = CreateService(isModuleAccessible: true);

        var result = await service.SavePolicy(new WorkFromHomePolicyDto
        {
            AllowManagerSelfApproval = true,
            ApplicableDepartments = [], ApplicableEmployeeIds = [], ExcludedEmployeeIds = [], ApplicableEmploymentTypes = []
        });

        Assert.False(result.Success);
        Assert.StartsWith("WFH_SELF_APPROVAL_UNSUPPORTED:", result.Message);
        _policyRepo.Verify(x => x.AddOne(It.IsAny<WorkFromHomePolicy>()), Times.Never);
        _policyRepo.Verify(x => x.Update(It.IsAny<FilterDefinition<WorkFromHomePolicy>>(), It.IsAny<WorkFromHomePolicy>()), Times.Never);
    }

    [Fact]
    public async Task SavePolicy_WhenPunchWindowIsOutsideBusinessDay_ReturnsValidationFailureWithoutWriting()
    {
        var service = CreateService(isModuleAccessible: true);

        var result = await service.SavePolicy(new WorkFromHomePolicyDto
        {
            AllowedCheckInFrom = TimeSpan.FromHours(25)
        });

        Assert.False(result.Success);
        Assert.StartsWith("WFH_INVALID_POLICY:", result.Message);
        _policyRepo.Verify(x => x.AddOne(It.IsAny<WorkFromHomePolicy>()), Times.Never);
        _policyRepo.Verify(x => x.Update(It.IsAny<FilterDefinition<WorkFromHomePolicy>>(), It.IsAny<WorkFromHomePolicy>()), Times.Never);
    }

    [Theory]
    [InlineData(4, 5, false)]
    [InlineData(0, 1, true)]
    public async Task SaveEmployeeAllocation_UsesTenantAndPolicyVersionCompareAndSet(int previousVersion, int expectedVersion, bool isLegacyPolicy)
    {
        var service = CreateService(isModuleAccessible: true);
        var policy = new WorkFromHomePolicy
        {
            CompanyId = "comp-123",
            PolicyId = "policy-1",
            Version = previousVersion,
            MaxDaysPerWeek = 3,
            EmployeeAllocations = [],
            ApplicableEmployeeIds = [],
            ExcludedEmployeeIds = []
        };
        _employeeRepo.Setup(x => x.Exist(It.IsAny<System.Linq.Expressions.Expression<Func<EmpUser, bool>>>(), false))
            .ReturnsAsync(true);
        _policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<WorkFromHomePolicy, bool>>>()))
            .ReturnsAsync(policy);
        FilterDefinition<WorkFromHomePolicy>? updateFilter = null;
        _policyRepo.Setup(x => x.Update(It.IsAny<FilterDefinition<WorkFromHomePolicy>>(), It.IsAny<WorkFromHomePolicy>()))
            .Callback<FilterDefinition<WorkFromHomePolicy>, WorkFromHomePolicy>((filter, _) => updateFilter = filter)
            .ReturnsAsync(new Result { Success = true });

        var result = await service.SaveEmployeeAllocation(new WorkFromHomeEmployeeAllocationDto
        {
            EmployeeId = "EMP-1",
            WeeklyLimit = 2
        });

        Assert.True(result.Success);
        Assert.Equal(expectedVersion, policy.Version);
        Assert.NotNull(updateFilter);
        var renderedFilter = Render(updateFilter!);
        Assert.Contains("comp-123", renderedFilter.ToJson());
        Assert.Contains("policy-1", renderedFilter.ToJson());
        Assert.Contains("Version", renderedFilter.ToJson());
        if (isLegacyPolicy)
            Assert.Contains("$exists", renderedFilter.ToJson());
        else
            Assert.Contains(previousVersion.ToString(), renderedFilter.ToJson());
    }

    [Fact]
    public async Task GetEmployeeContext_WhenModuleDisabled_ReturnsModuleDisabledReason()
    {
        var service = CreateService(isModuleAccessible: false);

        var result = await service.GetEmployeeContext();

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.Equal("WFH_MODULE_DISABLED", result.MethodResult.DisabledReasonCode);
        Assert.False(result.MethodResult.IsEligible);
    }

    [Fact]
    public async Task SavePolicy_WhenModuleDisabled_ReturnsAccessError()
    {
        var service = CreateService(isModuleAccessible: false);

        var result = await service.SavePolicy(new WorkFromHomePolicyDto { IsEnabled = true, MaxDaysPerWeek = 2 });

        Assert.False(result.Success);
        Assert.Contains("disabled", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveEmployeeAllocation_WhenModuleDisabled_ReturnsAccessError()
    {
        var service = CreateService(isModuleAccessible: false);

        var result = await service.SaveEmployeeAllocation(new WorkFromHomeEmployeeAllocationDto { EmployeeId = "EMP101", WeeklyLimit = 2 });

        Assert.False(result.Success);
        Assert.Contains("disabled", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetEmployeeAllocations_WhenModuleDisabled_ReturnsEmptyList()
    {
        var service = CreateService(isModuleAccessible: false);

        var result = await service.GetEmployeeAllocations();

        Assert.True(result.Success);
        Assert.NotNull(result.MethodResult);
        Assert.Empty(result.MethodResult);
    }

    [Fact]
    public async Task GetMy_WhenModuleDisabled_ReturnsEmptyResults()
    {
        var service = CreateService(isModuleAccessible: false);

        var result = await service.GetMy();

        Assert.True(result.Success);
        Assert.Empty(result.MethodResults);
        Assert.Equal(0, result.TotalRecords);
    }

    [Fact]
    public async Task GetMy_WhenEnabled_UsesTenantAndUserScopeAndCapsPageSize()
    {
        var filterJson = Render(WorkFromHomeService.BuildMyRequestsFilter("comp-123", "user-456")).ToJson();
        Assert.Contains("comp-123", filterJson);
        Assert.Contains("user-456", filterJson);
        Assert.Equal(25, WorkFromHomeService.NormalizeWfhPageSize(10000));
        Assert.Equal(100, WorkFromHomeService.NormalizeWfhPageSize(100));
    }

    [Fact]
    public async Task GetAll_WhenModuleDisabled_ReturnsEmptyResults()
    {
        var service = CreateService(isModuleAccessible: false);

        var result = await service.GetAll(new WorkFromHomeRequestFilterDto());

        Assert.True(result.Success);
        Assert.Empty(result.MethodResults);
        Assert.Equal(0, result.TotalRecords);
    }

    [Fact]
    public async Task GetAll_WhenFilterIsNull_ReturnsBadRequestBeforeRepositoryAccess()
    {
        var service = CreateService(isModuleAccessible: true);

        var result = await service.GetAll(null!);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.StartsWith("WFH_INVALID_FILTER:", result.Message);
        _requestRepo.Verify(x => x.GetCollection(), Times.Never);
        _employeeRepo.Verify(x => x.GetCollection(), Times.Never);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(5000, false)]
    [InlineData(5001, true)]
    public void EmployeeSearchMatchLimit_RejectsOnlyResultsOverTheBound(int resultCount, bool isTooBroad)
    {
        Assert.Equal(isTooBroad, WorkFromHomeService.IsEmployeeFilterTooBroad(resultCount));
    }

    [Theory]
    [InlineData(100, false)]
    [InlineData(101, true)]
    public void EmployeeSearchLength_IsBounded(int length, bool isTooLong)
    {
        Assert.Equal(isTooLong, WorkFromHomeService.IsEmployeeSearchTooLong(new string('a', length)));
    }

    [Theory]
    [InlineData(100, false)]
    [InlineData(101, true)]
    public void DepartmentFilterLength_IsBounded(int length, bool isTooLong)
    {
        Assert.Equal(isTooLong, WorkFromHomeService.IsDepartmentFilterTooLong(new string('d', length)));
    }

    [Fact]
    public void EmployeeSearchLength_TrimsBeforeEnforcingTheBound()
    {
        Assert.False(WorkFromHomeService.IsEmployeeSearchTooLong($" {new string('a', 100)} "));
    }

    [Fact]
    public void TeamEmployeeFilter_IsTenantScopedAndEscapesSearchAndDepartmentInput()
    {
        var rendered = Render(WorkFromHomeService.BuildTeamEmployeeFilter("company-a", "a.*", "Ops.*"));

        Assert.Equal("company-a", rendered[nameof(EmpUser.CompanyId)].AsString);
        Assert.False(rendered[nameof(EmpUser.IsDeleted)].AsBoolean);
        Assert.Equal(@"^Ops\.\*$", rendered[nameof(EmpUser.Department)].AsBsonRegularExpression.Pattern);
        var searchAlternatives = rendered["$or"].AsBsonArray;
        foreach (var fieldName in new[] { nameof(EmpUser.EmployeeId), nameof(EmpUser.FirstName), nameof(EmpUser.LastName) })
        {
            var search = searchAlternatives
                .Select(x => x.AsBsonDocument)
                .Single(x => x.Contains(fieldName))[fieldName]
                .AsBsonRegularExpression;
            Assert.Equal(@"a\.\*", search.Pattern);
            Assert.Equal("i", search.Options);
        }
    }

    [Theory]
    [InlineData(true, true, false, false, true)]
    [InlineData(true, false, true, true, false)]
    [InlineData(false, true, true, false, false)]
    [InlineData(false, true, false, true, false)]
    [InlineData(false, true, true, true, true)]
    public void DecisionPermission_RequiresTeamApprovalAndExplicitCrossApproverOverride(
        bool assignedApprover, bool approveTeam, bool viewAll, bool overridePermission, bool expected)
    {
        Assert.Equal(expected, WorkFromHomeService.CanDecideRequest(
            assignedApprover, approveTeam, viewAll, overridePermission));
    }

    [Fact]
    public async Task Decide_ForAnotherApproversRequest_RequiresExplicitOverridePermission()
    {
        var service = CreateService(isModuleAccessible: true);
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<WorkFromHomeRequest, bool>>>() ))
            .ReturnsAsync(new WorkFromHomeRequest
            {
                CompanyId = "comp-123", RequestId = "req-override", UserId = "employee-1",
                Status = "Pending", ApproverUserId = "manager-1", Version = 2
            });
        _roles.Setup(x => x.VerifyUserAccess("Work_From_Home",
                It.Is<string[]>(permissions => permissions.Contains(Codeji.CMS.Utility.Constraints.Permission.Override)),
                "user-456", "comp-123", It.IsAny<UserCheckModel>()))
            .ReturnsAsync(false);

        var result = await service.Decide("req-override", "Approved", new WorkFromHomeDecisionDto { Version = 2 });

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
        Assert.StartsWith("WFH_APPROVER_NOT_AUTHORIZED:", result.Message);
        _policyRepo.Verify(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<WorkFromHomePolicy, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task GetTeam_WhenModuleDisabled_ReturnsEmptyResults()
    {
        var service = CreateService(isModuleAccessible: false);

        var result = await service.GetTeam(new WorkFromHomeRequestFilterDto());

        Assert.True(result.Success);
        Assert.Empty(result.MethodResults);
        Assert.Equal(0, result.TotalRecords);
    }

    [Fact]
    public async Task Decide_WhenModuleDisabled_ReturnsAccessError()
    {
        var service = CreateService(isModuleAccessible: false);

        var result = await service.Decide("req-1", "Approved", new WorkFromHomeDecisionDto { Version = 1 });

        Assert.False(result.Success);
        Assert.Contains("WFH_MODULE_DISABLED", result.Message);
    }

    [Fact]
    public async Task DecideApproval_WhenMongoTransactionsAreUnavailable_LeavesRequestPending()
    {
        var service = CreateService(isModuleAccessible: true);
        var request = new WorkFromHomeRequest
        {
            CompanyId = "comp-123", RequestId = "req-1", UserId = "employee-1", EmployeeId = "E-1",
            Status = "Pending", ApproverUserId = "user-456", Version = 1,
            FromDate = DateTime.UtcNow.Date.AddDays(5), ToDate = DateTime.UtcNow.Date.AddDays(5), DurationType = "FullDay"
        };
        _requestRepo.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<WorkFromHomeRequest, bool>>>() ))
            .ReturnsAsync(request);
        _requestRepo.Setup(x => x.Update(It.IsAny<FilterDefinition<WorkFromHomeRequest>>(), It.IsAny<WorkFromHomeRequest>()))
            .ReturnsAsync(new Result { Success = true });
        _policyRepo.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<WorkFromHomePolicy, bool>>>() ))
            .ReturnsAsync(new WorkFromHomePolicy { CompanyId = "comp-123", IsEnabled = true, Version = 1, MaxDaysPerWeek = 5, TimeZoneId = "UTC" });
        var employee = new EmpUser
        {
            CompanyId = "comp-123", UserId = "employee-1", EmployeeId = "E-1", FirstName = "Test", LastName = "Employee",
            Email = "test@example.com", Password = "x", Gender = "", JobRole = "", Department = "Sales", ReportingManager = "manager",
            PhoneNumber = "", BloodGroup = "", PersonalEmail = "", EmergencyContact = "", DateOfJoining = "2020-01-01",
            Address = "", ProfileUrl = "", Status = true
        };
        _employeeRepo.Setup(x => x.FirstOrDefault(It.IsAny<System.Linq.Expressions.Expression<Func<EmpUser, bool>>>() ))
            .ReturnsAsync(employee);
        _mongoClient.Setup(x => x.StartSessionAsync(It.IsAny<ClientSessionOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotSupportedException("Transactions are unavailable."));

        var result = await service.Decide("req-1", "Approved", new WorkFromHomeDecisionDto { Version = 1 });

        Assert.False(result.Success);
        Assert.Contains("WFH_TRANSACTION_REQUIRED", result.Message);
        Assert.Equal("Pending", request.Status);
        Assert.Equal(1, request.Version);
        _requestRepo.Verify(x => x.Update(It.IsAny<FilterDefinition<WorkFromHomeRequest>>(), It.IsAny<WorkFromHomeRequest>()), Times.Never);
        _requestRepo.Verify(x => x.AddOne(It.IsAny<WorkFromHomeRequest>()), Times.Never);
    }

    [Fact]
    public void QuotaLockId_IsDeterministicAndTenantScoped()
    {
        var first = WorkFromHomeService.BuildQuotaLockId("company-a", "user-1");
        Assert.Equal(first, WorkFromHomeService.BuildQuotaLockId("company-a", "user-1"));
        Assert.NotEqual(first, WorkFromHomeService.BuildQuotaLockId("company-b", "user-1"));
        Assert.NotEqual(first, WorkFromHomeService.BuildQuotaLockId("company-a", "user-2"));
        Assert.NotEqual(
            WorkFromHomeService.BuildQuotaLockId("a:b", "c"),
            WorkFromHomeService.BuildQuotaLockId("a", "b:c"));
    }

    [Fact]
    public void ApprovalEmployeeFilter_IsBoundToTenantUserAndCurrentEmploymentState()
    {
        var rendered = Render(WorkFromHomeService.BuildApprovalEmployeeFilter("company-a", "employee-user-1")).ToJson();

        Assert.Contains("company-a", rendered);
        Assert.Contains("employee-user-1", rendered);
        Assert.Contains("CompanyId", rendered);
        Assert.Contains("Status", rendered);
        Assert.Contains("IsDeleted", rendered);
        Assert.Contains("EmploymentStatus", rendered);
    }

    [Fact]
    public void DeadLetterOutboxFilter_IsTenantScopedAndExcludesDeletedAndNonDeadLetters()
    {
        var rendered = Render(WorkFromHomeService.BuildDeadLetterOutboxFilter("company-a")).ToJson();

        Assert.Contains("company-a", rendered);
        Assert.Contains("CompanyId", rendered);
        Assert.Contains("DeadLetter", rendered);
        Assert.Contains("IsDeleted", rendered);
    }

    [Theory]
    [InlineData("WFH_REQUEST", "wfh-request-1", "wfh-request-1")]
    [InlineData("LEAVE", "leave-request-1", null)]
    [InlineData("MANUAL", "manual-source-1", null)]
    [InlineData("WFH_REQUEST", "", null)]
    public void ResolveWfhSourceId_OnlyReturnsAValidWfhOwnedSource(string? sourceType, string? sourceId, string? expected)
    {
        Assert.Equal(expected, LeaveAttendanceReconciliationService.ResolveWfhSourceId(sourceType, sourceId));
    }

    [Fact]
    public async Task ReplayDeadLetterOutbox_RequiresReasonBeforeDatabaseWrite()
    {
        var service = CreateService(isModuleAccessible: true);

        var result = await service.ReplayDeadLetterOutbox("event-a", new WorkFromHomeDeadLetterReplayDto { Reason = "  " });

        Assert.False(result.Success);
        Assert.Contains("WFH_OUTBOX_REPLAY_REASON_REQUIRED", result.Message);
        _outboxRepo.Verify(x => x.GetCollection(), Times.Never);
    }

    [Fact]
    public async Task ReplayDeadLetterOutbox_UsesTenantStatusCasAndRecordsActorAndReason()
    {
        var collection = new Mock<IMongoCollection<WorkFromHomeOutboxEvent>>();
        _outboxRepo.Setup(x => x.GetCollection()).Returns(collection.Object);
        FilterDefinition<WorkFromHomeOutboxEvent>? capturedFilter = null;
        UpdateDefinition<WorkFromHomeOutboxEvent>? capturedUpdate = null;
        collection.Setup(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<WorkFromHomeOutboxEvent>>(),
                It.IsAny<UpdateDefinition<WorkFromHomeOutboxEvent>>(), It.IsAny<UpdateOptions>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<WorkFromHomeOutboxEvent>, UpdateDefinition<WorkFromHomeOutboxEvent>, UpdateOptions, CancellationToken>((filter, update, _, _) =>
            {
                capturedFilter = filter;
                capturedUpdate = update;
            })
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        var service = CreateService(isModuleAccessible: true, companyId: "company-a", userId: "hr-user-a");

        var result = await service.ReplayDeadLetterOutbox("event-a", new WorkFromHomeDeadLetterReplayDto { Reason = "Verified downstream SMTP recovery" });

        Assert.True(result.Success);
        var filterJson = Render(capturedFilter!).ToJson();
        Assert.Contains("company-a", filterJson);
        Assert.Contains("event-a", filterJson);
        Assert.Contains("DeadLetter", filterJson);
        var updateJson = capturedUpdate!.Render(new RenderArgs<WorkFromHomeOutboxEvent>(BsonSerializer.SerializerRegistry.GetSerializer<WorkFromHomeOutboxEvent>(), BsonSerializer.SerializerRegistry)).ToJson();
        Assert.Contains("Pending", updateJson);
        Assert.Contains("hr-user-a", updateJson);
        Assert.Contains("Verified downstream SMTP recovery", updateJson);
        Assert.Contains("ReplayHistory", updateJson);
    }

    [Fact]
    public async Task Cancel_WhenModuleDisabled_ReturnsAccessError()
    {
        var service = CreateService(isModuleAccessible: false);

        var result = await service.Cancel("req-1", new WorkFromHomeCancelDto { Version = 1 });

        Assert.False(result.Success);
        Assert.Contains("WFH_MODULE_DISABLED", result.Message);
    }

    [Fact]
    public async Task GetTiming_WhenModuleDisabled_ReturnsAccessError()
    {
        var service = CreateService(isModuleAccessible: false);

        var result = await service.GetTiming("req-1");

        Assert.False(result.Success);
        Assert.Contains("WFH_MODULE_DISABLED", result.Message);
    }
}
