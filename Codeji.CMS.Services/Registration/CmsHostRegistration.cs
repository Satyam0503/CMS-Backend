using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.GenericRepository.Registration;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Interfaces;
using Codeji.CMS.Repository.Repositories;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.CareerPortal;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.Interfaces;
using Codeji.CMS.Services.PayRoll;
using Codeji.CMS.Services.PayRoll.Interface;
using Codeji.CMS.Services.Recruitments;
using Codeji.CMS.Utility.Helpers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
using Company = Codeji.CMS.Repository.Entities.Company;

namespace Codeji.CMS.Services.Registration;

/// <summary>Registrations shared by every host (API and Scheduler). Host-specific pieces such as INotificationService stay in the host.</summary>
public static class CmsHostRegistration
{
    public static IServiceCollection AddCmsCore(this IServiceCollection services, IConfiguration configuration)
    {
        try
        {
            BsonSerializer.RegisterSerializer(new EnumSerializer<NotificationPreferenceType>(BsonType.String));
        }
        catch (BsonSerializationException)
        {
            // Already registered: the registry is process-wide and test hosts re-run startup.
        }

        var mongoConnection = configuration.GetConnectionString("mongodb")
            ?? throw new Exception("ConnectionStrings:mongodb is not configured.");
        var mongoUrl = MongoUrl.Create(mongoConnection);
        if (string.IsNullOrEmpty(mongoUrl.DatabaseName))
            throw new Exception("ConnectionStrings:mongodb must include the database name in the path.");

        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnection));
        services.AddScoped<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoUrl.DatabaseName));

        // Public application idempotency must be backed by a database uniqueness
        // guarantee before background workers or API requests start processing.
        services.AddHostedService<RecruitmentIndexInitializer>();

        ConfigManager.Initialize(configuration);

        services.AddSingleton<IPriorityTaskQueue, PriorityTaskQueue>();
        services.AddHostedService<PriorityQueuedHostedService>();
        services.AddHttpContextAccessor();
        services.AddBusinessServices();
        services.AddHttpClient();
        services.AddRepositoryServices();

        var mapsterConfig = TypeAdapterConfig.GlobalSettings;
        mapsterConfig.Scan(typeof(MapsterConfig).Assembly);
        services.AddSingleton(mapsterConfig);
        services.AddScoped<IMapper, ServiceMapper>();
        services.AddTransient<PdfService>();
        services.AddScoped<MongoJobLock>();

        services.AddSingleton<IEmployeePresenceService, EmployeePresenceService>();

        services.AddScoped<IMongoDbRepository<EmpUser>, MongoRepository<EmpUser>>();
        services.AddScoped<IMongoDbRepository<EmployeeLifecyclePolicy>, MongoRepository<EmployeeLifecyclePolicy>>();
        services.AddScoped<IMongoDbRepository<EmployeeLifecycleCase>, MongoRepository<EmployeeLifecycleCase>>();
        services.AddScoped<IEmployeeLifecycleCaseService, EmployeeLifecycleCaseService>();
        services.AddScoped<IEmployeeLifecyclePolicyService, EmployeeLifecyclePolicyService>();
        services.AddScoped<IMongoDbRepository<EmpPayRoll>, MongoRepository<EmpPayRoll>>();
        services.AddScoped<IMongoDbRepository<PayrollRun>, MongoRepository<PayrollRun>>();
        services.AddScoped<IMongoDbRepository<PayrollRunEmployee>, MongoRepository<PayrollRunEmployee>>();
        services.AddScoped<IMongoDbRepository<PayrollPaymentBatch>, MongoRepository<PayrollPaymentBatch>>();
        services.AddScoped<IMongoDbRepository<PayrollAdjustment>, MongoRepository<PayrollAdjustment>>();
        services.AddScoped<IMongoDbRepository<EmployeeTaxDeclaration>, MongoRepository<EmployeeTaxDeclaration>>();
        services.AddScoped<IMongoDbRepository<PayrollPolicyProfile>, MongoRepository<PayrollPolicyProfile>>();
        services.AddScoped<IMongoDbRepository<SalaryTemplate>, MongoRepository<SalaryTemplate>>();
        services.AddScoped<IMongoDbRepository<PayrollPayGroup>, MongoRepository<PayrollPayGroup>>();
        services.AddScoped<IMongoDbRepository<PayrollSalaryComponentDefinition>, MongoRepository<PayrollSalaryComponentDefinition>>();
        services.AddScoped<IMongoDbRepository<EmployeePayrollAssignment>, MongoRepository<EmployeePayrollAssignment>>();
        services.AddScoped<IMongoDbRepository<EmployeeSalaryStructure>, MongoRepository<EmployeeSalaryStructure>>();
        services.AddScoped<IMongoDbRepository<EmployeeSalaryImportBatch>, MongoRepository<EmployeeSalaryImportBatch>>();
        services.AddScoped<IMongoDbRepository<PayrollDivisorPolicy>, MongoRepository<PayrollDivisorPolicy>>();
        services.AddScoped<IMongoDbRepository<MonthlyAttendanceSummary>, MongoRepository<MonthlyAttendanceSummary>>();
        services.AddScoped<IMongoDbRepository<Company.Company>, MongoRepository<Company.Company>>();
        services.AddScoped<IMongoDbRepository<Company.Department>, MongoRepository<Company.Department>>();
        services.AddScoped<IMongoDbRepository<Company.JobTitles>, MongoRepository<Company.JobTitles>>();

        services.AddScoped<IMongoDbRepository<RefreshToken>, MongoRepository<RefreshToken>>();
        services.AddScoped<IMongoDbRepository<EmployeeExit>, MongoRepository<EmployeeExit>>();
        services.AddScoped<IMongoDbRepository<EmployeeExitPolicy>, MongoRepository<EmployeeExitPolicy>>();
        services.AddScoped<IMongoDbRepository<EmployeeExitClearanceTemplate>, MongoRepository<EmployeeExitClearanceTemplate>>();
        services.AddScoped<IMongoDbRepository<EmployeeFinalSettlement>, MongoRepository<EmployeeFinalSettlement>>();
        services.AddScoped<IMongoDbRepository<EmployeeExitAttendanceCutoff>, MongoRepository<EmployeeExitAttendanceCutoff>>();
        services.AddScoped<IMongoDbRepository<EmployeeExitOutbox>, MongoRepository<EmployeeExitOutbox>>();
        services.AddScoped<IMongoDbRepository<FormerEmployeeAccessToken>, MongoRepository<FormerEmployeeAccessToken>>();
        services.AddSingleton<IEmployeeExitEvidenceStorage, PrivateEmployeeExitEvidenceStorage>();
        services.AddScoped<IFormerEmployeePortalService, FormerEmployeePortalService>();
        services.AddScoped<IMongoDbRepository<LeaveRequest>, MongoRepository<LeaveRequest>>();
        services.AddScoped<IMongoDbRepository<WorkFromHomeRequest>, MongoRepository<WorkFromHomeRequest>>();

        services.AddScoped<MongoDbContext>();
        services.AddScoped<ISalaryRepository, SalaryRepository>();
        services.AddScoped<ISalaryService, SalaryService>();

        services.AddScoped<IPayRollServices, PayRollServices>();
        services.AddSingleton<IProfessionalTaxResolver, ProfessionalTaxResolver>();
        services.AddScoped<ITaxDeclarationService, TaxDeclarationService>();
        services.AddScoped<IPayrollReadinessService, PayrollReadinessService>();
        services.AddScoped<IPayrollTemplateService, PayrollTemplateService>();
        services.AddScoped<IPayrollConfigurationService, PayrollConfigurationService>();
        services.AddScoped<IEmployeeSalaryStructureService, EmployeeSalaryStructureService>();
        services.AddScoped<IEmployeeSalaryImportService, EmployeeSalaryImportService>();
        services.AddScoped<IPayrollRunService, PayrollRunService>();
        services.AddScoped<IPayrollRunWorkflowService, PayrollRunWorkflowService>();
        services.AddScoped<IPayrollRunCalculationService, PayrollRunCalculationService>();
        services.AddScoped<IPayrollRunReviewService, PayrollRunReviewService>();

        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IAdminAttendanceService, AdminAttendanceService>();
        services.AddScoped<IAutoPayRollServices, AutoPayrollServices>();
        services.AddScoped<ICompanyService, CompanyService>();

        return services;
    }

    /// <summary>Timed jobs and outbox pollers. Exactly one host per environment should register these.</summary>
    public static IServiceCollection AddCmsBackgroundJobs(this IServiceCollection services)
    {
        services.AddHostedService<BirthDayAndAnniversaryNotificationHostedServices>();
        services.AddHostedService<LeaveAccrualHostedService>();
        services.AddHostedService<CareerNotificationWorker>();
        services.AddHostedService<RecruitmentEmailOutboxWorker>();
        services.AddHostedService<RecruitmentScreeningStageSyncWorker>();
        services.AddHostedService<RecruitmentHiringDecisionStageSyncWorker>();
        services.AddHostedService<RecruitmentRetentionWorker>();
        services.AddHostedService<RecruitmentResumeOrphanCleanupWorker>();
        services.AddHostedService<RecruitmentResumeScanMigrationWorker>();
        services.AddHostedService<AttendanceOutboxWorker>();
        services.AddHostedService<WorkFromHomeOutboxWorker>();
        services.AddHostedService<AttendanceReminderHostedService>();
        services.AddHostedService<EmployeeExitOutboxWorker>();
        services.AddHostedService<LeaveAttendanceReconciliationHostedService>();
        services.AddHostedService<PayrollHostedService>();
        return services;
    }
}
