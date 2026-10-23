using Codeji.CMS.Services.Account;
using Codeji.CMS.Services.Account.Interface;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Calendar;
using Codeji.CMS.Services.Calendar.Interface;
using Codeji.CMS.Services.CareerPortal;
using Codeji.CMS.Services.CareerPortal.Interface;
using Codeji.CMS.Services.Companies;
using Codeji.CMS.Services.Dashboard;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.LeaveManagement;
using Codeji.CMS.Services.NoticeBoard;
using Codeji.CMS.Services.PayRoll;
using Codeji.CMS.Services.PayRoll.Interface;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Microsoft.Extensions.DependencyInjection;
namespace Codeji.CMS.Services.Registration;

public static class ServicesRegistration
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        //   services.AddSingleton<IColleagueBusiness, ColleagueBusiness>();
        services.AddScoped<IMiddlewareService, MiddlewareService>();
        services.AddScoped<IApplicantsService, ApplicantServices>();
        services.AddScoped<IRecruitmentCandidateShareService, RecruitmentCandidateShareService>();
        services.AddScoped<IRecruitmentApplicationService, RecruitmentApplicationService>();
        services.AddScoped<IRecruitmentHandoffService, RecruitmentHandoffService>();
        services.AddScoped<IRecruitmentOnboardingService, RecruitmentOnboardingService>();
        services.AddScoped<IRecruitmentInterviewService, RecruitmentInterviewService>();
        services.AddScoped<IRecruitmentInterviewDebriefService, RecruitmentInterviewDebriefService>();
        services.AddScoped<IRecruitmentInterviewPlanService, RecruitmentInterviewPlanService>();
        services.AddScoped<IRecruitmentOfferService, RecruitmentOfferService>();
        services.AddScoped<IRecruitmentOfferApprovalPolicyService, RecruitmentOfferApprovalPolicyService>();
        services.AddScoped<IRecruitmentRequisitionService, RecruitmentRequisitionService>();
        services.AddScoped<IRecruitmentScorecardService, RecruitmentScorecardService>();
        services.AddScoped<IRecruitmentScreeningService, RecruitmentScreeningService>();
        services.AddScoped<IRecruitmentScreeningQueueService, RecruitmentScreeningQueueService>();
        services.AddScoped<IRecruitmentHiringDecisionService, RecruitmentHiringDecisionService>();
        services.AddScoped<IRecruitmentCandidatePrivacyService, RecruitmentCandidatePrivacyService>();
        services.AddScoped<IRecruitmentResumeExtractionService, RecruitmentResumeExtractionService>();
        services.AddScoped<IRecruitmentAuditExportService, RecruitmentAuditExportService>();
        services.AddScoped<IRecruitmentTalentPoolService, RecruitmentTalentPoolService>();
        services.AddScoped<IRecruitmentDuplicateDetectionService, RecruitmentDuplicateDetectionService>();
        services.AddScoped<IRecruitmentEnterpriseService, RecruitmentEnterpriseService>();
        services.AddScoped<IRecruitmentExternalActionService, RecruitmentExternalActionService>();
        services.AddScoped<IRecruitmentCandidateActionService, RecruitmentCandidateActionService>();
        services.AddScoped<IRecruitmentEmailOutboxProcessor, RecruitmentEmailOutboxProcessor>();
        services.AddScoped<IRecruitmentOfferNegotiationService, RecruitmentOfferNegotiationService>();
        services.AddScoped<IPublicCareerService, PublicCareerService>();
        services.AddScoped<IPublicCompanyProfileService, PublicCompanyProfileService>();
        services.AddScoped<ICareerSubscriptionService, CareerSubscriptionService>();
        services.AddScoped<ICareerEngagementService, CareerEngagementService>();
        services.AddScoped<ICareerNotificationService, CareerNotificationService>();
        services.AddScoped<IRoleService, RoleServices>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IEmployeeExitOutboxProcessor, EmployeeExitOutboxProcessor>();
        services.AddScoped<IJobVacancy, JobVacancyService>();
        services.AddScoped<ICompanyMasterService, CompanyMasterService>();
        services.AddScoped<IDashboardService, DashboardServices>();
        services.AddScoped<INoticeBoardService, NoticeBoardServices>();
        services.AddScoped<ICalendarServices, CalendarServices>();
        services.AddScoped<ILeaveManagementService, LeaveManagementService>();
        services.AddScoped<IAccountServices, AccountServices>();
        services.AddScoped<IPayRollServices, PayRollServices>();
        services.AddScoped<IAttendanceStatusService, AttendanceStatusService>();
        services.AddScoped<IAttendanceImportService, AttendanceImportService>();
        services.AddScoped<IAttendancePenaltyService, AttendancePenaltyService>();
        services.AddScoped<IWeeklyOffService, WeeklyOffService>();
        services.AddScoped<IAttendanceEditGuard, AttendanceEditGuard>();
        services.AddScoped<IEffectiveOfficeScheduleService, EffectiveOfficeScheduleService>();
        services.AddScoped<IShiftAttendanceReconciliationService, ShiftAttendanceReconciliationService>();
        services.AddScoped<IAttendanceStatusCalculationService, AttendanceStatusCalculationService>();
        services.AddScoped<IOfficeScheduleSettingsService, OfficeScheduleSettingsService>();
        services.AddScoped<IOfficeScheduleAssignmentService, OfficeScheduleAssignmentService>();
        services.AddScoped<IAttendanceMutationValidator, AttendanceMutationValidator>();
        services.AddScoped<IAttendanceInitializationService, AttendanceInitializationService>();
        services.AddScoped<IAttendanceAuditWriter, AttendanceAuditWriter>();
        services.AddScoped<IAttendanceOutboxProcessor, AttendanceOutboxProcessor>();
        services.AddScoped<IAttendanceReminderProcessor, AttendanceReminderProcessor>();
        services.AddScoped<IAttendancePayrollReadinessService, AttendancePayrollReadinessService>();
        services.AddScoped<ICompanyWorkingCalendarService, CompanyWorkingCalendarService>();
        services.AddScoped<ILeaveAttendanceReconciliationService, LeaveAttendanceReconciliationService>();
        services.AddScoped<ILifecycleAttendanceReconciliationService, LifecycleAttendanceReconciliationService>();
        services.AddScoped<IWorkFromHomeService, WorkFromHomeService>();
        services.AddScoped<IPayrollDivisorPolicyService, PayrollDivisorPolicyService>();
        services.AddScoped<IPayrollConfigurationService, PayrollConfigurationService>();
        services.AddScoped<IPayrollTemplateService, PayrollTemplateService>();
        services.AddScoped<IEmployeeSalaryStructureService, EmployeeSalaryStructureService>();
        services.AddScoped<IEmployeeSalaryImportService, EmployeeSalaryImportService>();
        services.AddScoped<IPayrollRunService, PayrollRunService>();
        services.AddScoped<IPayrollRunWorkflowService, PayrollRunWorkflowService>();
        services.AddScoped<IPayrollRunCalculationService, PayrollRunCalculationService>();
        services.AddScoped<IPayrollRunReviewService, PayrollRunReviewService>();
        services.AddScoped<IPayrollPaymentBatchService, PayrollPaymentBatchService>();
        services.AddScoped<IPayrollAdjustmentService, PayrollAdjustmentService>();
        services.AddScoped<IPayrollReportsService, PayrollReportsService>();
        return services;
    }
}

















