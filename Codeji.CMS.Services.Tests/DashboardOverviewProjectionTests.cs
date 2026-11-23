using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Dashboard;
using Codeji.CMS.Services.Recruitments;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Tests;

public sealed class DashboardOverviewProjectionTests
{
    [Fact]
    public void EmployeeAnalyticsProjection_RequestsOnlyFieldsUsedByDashboardMetrics()
    {
        var rendered = DashboardOverviewService.EmployeeAnalyticsProjection.Render(
            new RenderArgs<EmpUser>(
                BsonSerializer.LookupSerializer<EmpUser>(),
                BsonSerializer.SerializerRegistry,
                new PathRenderArgs(),
                renderDollarForm: false,
                renderForFind: false,
                renderForElemMatch: false,
                translationOptions: null));
        var projection = rendered.Document;
        var text = projection.ToString();

        Assert.Contains("FirstName", text);
        Assert.Contains("DateOfJoining", text);
        Assert.Contains("Lifecycle", text);
        Assert.Contains("REJOIN", text);
        Assert.DoesNotContain("Password", text);
        Assert.Contains("\"Email\" : \"\"", text); // required EmpUser member is a constant placeholder, never sourced from Mongo.
        Assert.DoesNotContain("\"Email\" : \"$Email\"", text);
        Assert.DoesNotContain("PhoneNumber", text);
        Assert.DoesNotContain("PersonalEmail", text);
        Assert.DoesNotContain("PanNumber", text);
        Assert.DoesNotContain("BankAccountNumber", text);
        Assert.DoesNotContain("Address", text);
        Assert.DoesNotContain("CustomAttributeList", text);
    }

    [Fact]
    public void AttendanceAnalyticsProjection_ExcludesEmployeeNotesAndAuditMetadata()
    {
        var rendered = DashboardOverviewService.AttendanceAnalyticsProjection.Render(
            new RenderArgs<AttendanceModel>(
                BsonSerializer.LookupSerializer<AttendanceModel>(),
                BsonSerializer.SerializerRegistry,
                new PathRenderArgs(),
                renderDollarForm: false,
                renderForFind: false,
                renderForElemMatch: false,
                translationOptions: null));
        var text = rendered.Document.ToString();

        Assert.Contains("UserId", text);
        Assert.Contains("CheckInTime", text);
        Assert.Contains("Status", text);
        Assert.DoesNotContain("Remarks", text);
        Assert.DoesNotContain("RemarkText", text);
        Assert.DoesNotContain("OperatorRemark", text);
        Assert.DoesNotContain("MarkedByUserId", text);
        Assert.DoesNotContain("ModifiedByUserId", text);
        Assert.DoesNotContain("SourceId", text);
    }

    [Fact]
    public void LeaveAnalyticsProjection_ExcludesReasonsCommentsEvidenceAndReconciliationDetails()
    {
        var rendered = DashboardOverviewService.LeaveAnalyticsProjection.Render(
            new RenderArgs<LeaveRequest>(
                BsonSerializer.LookupSerializer<LeaveRequest>(),
                BsonSerializer.SerializerRegistry,
                new PathRenderArgs(),
                renderDollarForm: false,
                renderForFind: false,
                renderForElemMatch: false,
                translationOptions: null));
        var text = rendered.Document.ToString();

        Assert.Contains("EmployeeId", text);
        Assert.Contains("LeavePolicyId", text);
        Assert.Contains("StartDate", text);
        Assert.Contains("EndDate", text);
        Assert.Contains("Status", text);
        Assert.Contains("CreatedDate", text);
        Assert.Contains("ReviewedAt", text);
        Assert.Contains("\"Reason\" : \"\"", text); // required member is an empty constant, not the stored reason.
        Assert.DoesNotContain("\"Reason\" : \"$Reason\"", text);
        Assert.DoesNotContain("Comment", text);
        Assert.DoesNotContain("Evidence", text);
        Assert.DoesNotContain("ReconciliationError", text);
        Assert.DoesNotContain("AssignedReviewer", text);
    }

    [Fact]
    public void ExitActivityProjection_ExcludesOffboardingCaseDetails()
    {
        var rendered = DashboardOverviewService.ExitActivityProjection.Render(
            new RenderArgs<EmployeeExit>(
                BsonSerializer.LookupSerializer<EmployeeExit>(),
                BsonSerializer.SerializerRegistry,
                new PathRenderArgs(),
                renderDollarForm: false,
                renderForFind: false,
                renderForElemMatch: false,
                translationOptions: null));
        var text = rendered.Document.ToString();

        Assert.Contains("EmployeeUserId", text);
        Assert.Contains("ExitType", text);
        Assert.Contains("InitiatedAtUtc", text);
        Assert.DoesNotContain("Reason", text);
        Assert.DoesNotContain("Notes", text);
        Assert.DoesNotContain("Discussion", text);
        Assert.DoesNotContain("ResignationAttachment", text);
        Assert.DoesNotContain("Evidence", text);
        Assert.DoesNotContain("Clearances", text);
        Assert.DoesNotContain("FinanceItems", text);
        Assert.DoesNotContain("Documents", text);
    }

    [Fact]
    public void VacancyActivityProjection_ExcludesRecruitmentAndCompensationDetails()
    {
        var rendered = DashboardOverviewService.VacancyActivityProjection.Render(
            new RenderArgs<JobVacancy>(
                BsonSerializer.LookupSerializer<JobVacancy>(),
                BsonSerializer.SerializerRegistry,
                new PathRenderArgs(),
                renderDollarForm: false,
                renderForFind: false,
                renderForElemMatch: false,
                translationOptions: null));
        var text = rendered.Document.ToString();

        Assert.Contains("CreatedBy", text);
        Assert.Contains("Title", text);
        Assert.Contains("CreatedDate", text);
        Assert.DoesNotContain("SalaryMin", text);
        Assert.DoesNotContain("SalaryMax", text);
        Assert.DoesNotContain("RecruiterContactEmail", text);
        Assert.DoesNotContain("Description", text);
        Assert.DoesNotContain("Responsibilities", text);
        Assert.DoesNotContain("ScreeningCriteria", text);
    }

    [Fact]
    public void ExitMovementProjection_ExcludesOffboardingCaseAndEmployeeDetails()
    {
        var rendered = DashboardOverviewService.ExitMovementProjection.Render(
            new RenderArgs<EmployeeExit>(
                BsonSerializer.LookupSerializer<EmployeeExit>(),
                BsonSerializer.SerializerRegistry,
                new PathRenderArgs(),
                renderDollarForm: false,
                renderForFind: false,
                renderForElemMatch: false,
                translationOptions: null));
        var text = rendered.Document.ToString();

        Assert.Contains("Status", text);
        Assert.Contains("LastWorkingDate", text);
        Assert.Contains("CompletedAtUtc", text);
        Assert.Contains("IsDeleted", text);
        Assert.DoesNotContain("EmployeeUserId", text);
        Assert.DoesNotContain("Reason", text);
        Assert.DoesNotContain("Notes", text);
        Assert.DoesNotContain("Discussion", text);
        Assert.DoesNotContain("Clearances", text);
        Assert.DoesNotContain("FinanceItems", text);
        Assert.DoesNotContain("Documents", text);
    }

    [Fact]
    public void JobTitleLabelProjection_RequestsOnlyIdsAndLocalizedLabels()
    {
        var rendered = DashboardOverviewService.JobTitleLabelProjection.Render(
            new RenderArgs<JobTitles>(
                BsonSerializer.LookupSerializer<JobTitles>(),
                BsonSerializer.SerializerRegistry,
                new PathRenderArgs(),
                renderDollarForm: false,
                renderForFind: false,
                renderForElemMatch: false,
                translationOptions: null));
        var text = rendered.Document.ToString();

        Assert.Contains("\"_id\" : \"$_id\"", text); // JobTitleId is stored as MongoDB's _id field.
        Assert.Contains("Titles", text);
        Assert.Contains("Language", text);
        Assert.Contains("Label", text);
        Assert.DoesNotContain("DepartmentId", text);
        Assert.DoesNotContain("IsActive", text);
        Assert.DoesNotContain("CompanyId", text);
    }

    [Fact]
    public void LeavePolicyNameProjection_ExcludesPolicyConfiguration()
    {
        var rendered = DashboardOverviewService.LeavePolicyNameProjection.Render(
            new RenderArgs<LeavePolicy>(
                BsonSerializer.LookupSerializer<LeavePolicy>(),
                BsonSerializer.SerializerRegistry,
                new PathRenderArgs(),
                renderDollarForm: false,
                renderForFind: false,
                renderForElemMatch: false,
                translationOptions: null));
        var text = rendered.Document.ToString();

        Assert.Contains("Name", text);
        Assert.Contains("\"Code\" : \"\"", text);
        Assert.DoesNotContain("\"Code\" : \"$Code\"", text);
        Assert.DoesNotContain("AccrualAmount", text);
        Assert.DoesNotContain("ApplicableTo", text);
        Assert.DoesNotContain("WorkFromHome", text);
        Assert.DoesNotContain("Lifecycle", text);
        Assert.DoesNotContain("Description", text);
    }

    [Fact]
    public void RecruitmentAnalyticsApplicantProjection_ExcludesCandidateContactAndResumeData()
    {
        var rendered = RecruitmentEnterpriseService.AnalyticsApplicantProjection.Render(
            new RenderArgs<Applicant>(
                BsonSerializer.LookupSerializer<Applicant>(),
                BsonSerializer.SerializerRegistry,
                new PathRenderArgs(),
                renderDollarForm: false,
                renderForFind: false,
                renderForElemMatch: false,
                translationOptions: null));
        var text = rendered.Document.ToString();

        Assert.Contains("_id", text);
        Assert.Contains("ActivityType", text);
        Assert.Contains("CreatedDate", text);
        Assert.Contains("IsDeleted", text);
        Assert.Contains("MergeStatus", text);
        Assert.Contains("CandidateReportedSource", text);
        Assert.Contains("\"Phone\" : \"\"", text);
        Assert.DoesNotContain("\"Phone\" : \"$Phone\"", text);
        Assert.DoesNotContain("Email", text);
        Assert.DoesNotContain("ResumeUrl", text);
        Assert.DoesNotContain("CoverLetter", text);
        Assert.DoesNotContain("PrivacyNoticeText", text);
        Assert.DoesNotContain("CandidateReportedSourceDetail", text);
        Assert.DoesNotContain("AttributionCampaign", text);
        Assert.DoesNotContain("FinalInterviewScore", text);
    }

    [Fact]
    public void RecruitmentAnalyticsApplicationProjection_ExcludesStageHistoryAndSnapshots()
    {
        var rendered = RecruitmentEnterpriseService.AnalyticsApplicationProjection.Render(
            new RenderArgs<RecruitmentJobApplication>(
                BsonSerializer.LookupSerializer<RecruitmentJobApplication>(),
                BsonSerializer.SerializerRegistry,
                new PathRenderArgs(),
                renderDollarForm: false,
                renderForFind: false,
                renderForElemMatch: false,
                translationOptions: null));
        var text = rendered.Document.ToString();

        Assert.Contains("ApplicantId", text);
        Assert.Contains("CurrentStage", text);
        Assert.Contains("UpdatedDate", text);
        Assert.DoesNotContain("PipelineSnapshot", text);
        Assert.DoesNotContain("StageHistory", text);
        Assert.DoesNotContain("LastCommandIdempotencyKey", text);
    }

    [Fact]
    public void RecruitmentAnalyticsOfferProjection_ExcludesCompensationAndApprovalDetails()
    {
        var rendered = RecruitmentEnterpriseService.AnalyticsOfferProjection.Render(
            new RenderArgs<RecruitmentOffer>(
                BsonSerializer.LookupSerializer<RecruitmentOffer>(),
                BsonSerializer.SerializerRegistry,
                new PathRenderArgs(),
                renderDollarForm: false,
                renderForFind: false,
                renderForElemMatch: false,
                translationOptions: null));
        var text = rendered.Document.ToString();

        Assert.Contains("ApplicantId", text);
        Assert.Contains("RespondedAtUtc", text);
        Assert.DoesNotContain("Salary", text);
        Assert.DoesNotContain("CompensationComponents", text);
        Assert.DoesNotContain("TotalCtc", text);
        Assert.DoesNotContain("ApprovalReference", text);
        Assert.DoesNotContain("StartDateUtc", text);
    }
}
