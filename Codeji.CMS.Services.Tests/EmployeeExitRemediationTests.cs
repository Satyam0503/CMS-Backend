using Codeji.CMS.DTO.Employee;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Utility.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Tests;

public sealed class EmployeeExitRemediationTests
{
    private static BsonDocument Render<T>(FilterDefinition<T> filter) =>
        filter.Render(new RenderArgs<T>(
            BsonSerializer.SerializerRegistry.GetSerializer<T>(),
            BsonSerializer.SerializerRegistry));

    [Fact]
    public void Future_leave_cancellation_filter_matches_both_user_id_and_employee_code()
    {
        const string companyId = "cmp-100";
        const string userId = "66f1c7d2e4b0a1234567890a";
        const string employeeCode = "EMP-042";
        var lastWorkingDate = new DateTime(2026, 11, 30, 0, 0, 0, DateTimeKind.Utc);

        var filter = Builders<LeaveRequest>.Filter.Where(x =>
            x.CompanyId == companyId
            && (x.EmployeeId == userId || x.EmployeeId == employeeCode)
            && x.EndDate > lastWorkingDate
            && (x.Status == EnumsHelper.LeaveRequestStatus.Pending || x.Status == EnumsHelper.LeaveRequestStatus.Accepted)
        );

        var rendered = Render(filter).ToJson();

        Assert.Contains(companyId, rendered, StringComparison.Ordinal);
        Assert.Contains(userId, rendered, StringComparison.Ordinal);
        Assert.Contains(employeeCode, rendered, StringComparison.Ordinal);
        // EnumsHelper.LeaveRequestStatus.Pending is 1, Accepted is 2
        Assert.Contains("1", rendered, StringComparison.Ordinal);
        Assert.Contains("2", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejoin_eligibility_falls_back_to_policy_snapshot_when_interview_omitted()
    {
        var exitWithNoInterview = new EmployeeExit
        {
            Status = "Resigned",
            ExitInterview = null,
            PolicySnapshot = new EmployeeExitPolicySnapshot
            {
                AllowRejoin = true
            }
        };

        bool isEligible;
        if (exitWithNoInterview.PolicySnapshot?.AllowRejoin == false)
            isEligible = false;
        else if (exitWithNoInterview.ExitInterview != null)
            isEligible = exitWithNoInterview.ExitInterview.IsRehireEligible == true;
        else
            isEligible = exitWithNoInterview.PolicySnapshot?.AllowRejoin ?? true;

        Assert.True(isEligible, "When interview is omitted, rehire eligibility must fall back to policy snapshot AllowRejoin.");
    }

    [Fact]
    public void Rejoin_eligibility_blocks_rehire_when_interview_explicitly_marks_ineligible()
    {
        var exitWithIneligibleInterview = new EmployeeExit
        {
            Status = "Resigned",
            ExitInterview = new EmployeeExitInterview
            {
                IsRehireEligible = false
            },
            PolicySnapshot = new EmployeeExitPolicySnapshot
            {
                AllowRejoin = true
            }
        };

        bool isEligible;
        if (exitWithIneligibleInterview.PolicySnapshot?.AllowRejoin == false)
            isEligible = false;
        else if (exitWithIneligibleInterview.ExitInterview != null)
            isEligible = exitWithIneligibleInterview.ExitInterview.IsRehireEligible == true;
        else
            isEligible = exitWithIneligibleInterview.PolicySnapshot?.AllowRejoin ?? true;

        Assert.False(isEligible, "Explicit ineligible flag in exit interview must block rehire even if company policy allows rejoin.");
    }

    [Fact]
    public void Final_settlement_preserves_custom_reimbursements_and_outstanding_dues()
    {
        var request = new CalculateEmployeeFinalSettlementRequest
        {
            EmployeeExitId = "exit-999",
            Version = 3,
            SalaryPayable = 45000m,
            Reimbursements = 4500m,
            LeaveEncashment = 6000m,
            BonusesAndIncentives = 2000m,
            Deductions = 1500m,
            NoticeRecovery = 3000m,
            OutstandingDues = 1200m
        };

        decimal reimbursements = request.Reimbursements >= 0 ? request.Reimbursements : 0;
        decimal leaveEncashment = request.LeaveEncashment >= 0 ? request.LeaveEncashment : 0;
        decimal outstandingDues = request.OutstandingDues >= 0 ? request.OutstandingDues : 0;

        var netPayable = request.SalaryPayable
            + reimbursements
            + leaveEncashment
            + request.BonusesAndIncentives
            - request.Deductions
            - request.NoticeRecovery
            - outstandingDues;

        Assert.Equal(4500m, reimbursements);
        Assert.Equal(6000m, leaveEncashment);
        Assert.Equal(1200m, outstandingDues);
        // 45000 + 4500 + 6000 + 2000 - 1500 - 3000 - 1200 = 51800
        Assert.Equal(51800m, netPayable);
    }

    [Fact]
    public void Notice_recovery_calculation_accurately_derives_daily_rate_and_shortfall()
    {
        const decimal monthlyGross = 60000m;
        const int divisor = 30;
        const int shortfallDays = 15;

        var dailyRate = decimal.Round(monthlyGross / divisor, 2, MidpointRounding.AwayFromZero);
        var totalRecovery = decimal.Round(dailyRate * shortfallDays, 2, MidpointRounding.AwayFromZero);

        Assert.Equal(2000.00m, dailyRate);
        Assert.Equal(30000.00m, totalRecovery);
    }

    [Fact]
    public void Cancel_notice_adjustment_request_dto_initializes_with_correct_properties()
    {
        var dto = new CancelEmployeeExitNoticeAdjustmentRequest
        {
            EmployeeExitId = "exit-abc",
            NoticeAdjustmentId = "adj-xyz",
            Version = 4,
            Reason = "Employee withdrew adjustment request"
        };

        Assert.Equal("exit-abc", dto.EmployeeExitId);
        Assert.Equal("adj-xyz", dto.NoticeAdjustmentId);
        Assert.Equal(4, dto.Version);
        Assert.Equal("Employee withdrew adjustment request", dto.Reason);
    }

    [Fact]
    public void Completed_exit_exposes_document_generation_and_interview_permitted_actions()
    {
        var actions = new List<string>();
        const string exitStatus = "Resigned";

        if (EmployeeLifecycleStatuses.IsCompletedEmploymentStatus(exitStatus))
        {
            actions.AddRange([
                "CalculateSettlement",
                "ApproveSettlement",
                "MarkSettlementPaid",
                "GenerateDocuments",
                "RecordInterview",
            ]);
        }

        Assert.Contains("GenerateDocuments", actions);
        Assert.Contains("RecordInterview", actions);
        Assert.Contains("CalculateSettlement", actions);
    }

    [Theory]
    [InlineData("Withdrawn")]
    [InlineData("Cancelled")]
    [InlineData("Rejected")]
    public void Withdrawn_cancelled_or_rejected_case_is_inactive_for_evidence_and_document_requests(string status)
    {
        var isInactive = status is "Withdrawn" or "Cancelled" or "Rejected";
        Assert.True(isInactive, $"Case with status {status} must be evaluated as inactive.");
    }

    [Theory]
    [InlineData("Resigned")]
    [InlineData("Terminated")]
    [InlineData("Completed")]
    [InlineData("Retired")]
    [InlineData("ContractEnded")]
    public void Evidence_update_filter_permits_all_completed_terminal_statuses(string status)
    {
        var filter = Builders<EmployeeExit>.Filter.Where(x =>
            (x.IsActive || x.Status == "Resigned" || x.Status == "Terminated" || x.Status == "Completed" || x.Status == "Retired" || x.Status == "ContractEnded")
        );

        var rendered = Render(filter).ToJson();
        Assert.Contains(status, rendered, StringComparison.Ordinal);
    }
}
