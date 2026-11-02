using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.API.ChatHub;
using Codeji.CMS.API.Notification;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.GenericRepository.Registration;
using Codeji.CMS.GenericRepository.Settings;
using Codeji.CMS.Services;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Employees;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Registration;
using Mapster;
using MapsterMapper;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
using Codeji.CMS.Services.Attendance;
using MongoDB.Driver;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Repositories;
using Codeji.CMS.Repository.Interfaces;
using Codeji.CMS.Services.Interfaces;
using Codeji.CMS.Services.PayRoll;
using Codeji.CMS.Services.PayRoll.Interface;
using Codeji.CMS.Services.Interface;
using System.Threading.RateLimiting;
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

try
{
    BsonSerializer.RegisterSerializer(
        new EnumSerializer<NotificationPreferenceType>(BsonType.String)
    );
}
catch (BsonSerializationException)
{
    // Already registered - happens when multiple WebApplicationFactory instances re-run
    // Program's top-level statements in the same test process; the registry is process-wide.
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("career-sensitive", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 12,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("account-sensitive", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("career-analytics", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
// .AddJsonOptions(options =>
// {
//     options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
// });

string corsName = "codeji";
builder.Services.AddCors(option => option.AddPolicy(corsName, builder =>
{
    builder
    .AllowCredentials()
    .SetIsOriginAllowed(origin =>
    {
        string host = new Uri(origin).Host;
        return host == "localhost" || host.EndsWith(".codeji.in");
    })
    .AllowAnyHeader()
    .AllowAnyMethod();
}));

// Swagger config
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("V2", new OpenApiInfo { Title = "Codeji Backend-Core API", Version = "V2" });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Only accept client-address information from the reverse proxies explicitly
    // configured for this deployment. With an empty list, forwarded headers are
    // ignored rather than allowing a caller to choose its own limiter partition.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                               ForwardedHeaders.XForwardedHost |
                               ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = builder.Configuration.GetValue<int?>("ReverseProxy:ForwardLimit") ?? 1;

    foreach (var proxy in builder.Configuration.GetSection("ReverseProxy:TrustedProxies").Get<string[]>() ?? [])
    {
        if (IPAddress.TryParse(proxy, out var address))
        {
            options.KnownProxies.Add(address);
        }
        else
        {
            throw new InvalidOperationException($"ReverseProxy:TrustedProxies contains an invalid IP address: '{proxy}'.");
        }
    }
});

// SignalR and Notification Services
builder.Services.AddSingleton<IUserIdProvider, GetUserIdProvider>();
builder.Services.AddSingleton<NotificationHub>();
builder.Services.AddTransient<INotificationService, NotificationService>();
builder.Services.AddSingleton<AntiforgeryMiddleware>();
builder.Services.AddSingleton<IPriorityTaskQueue, PriorityTaskQueue>();
builder.Services.AddHostedService<PriorityQueuedHostedService>();
builder.Services.AddHostedService<BirthDayAndAnniversaryNotificationHostedServices>();
builder.Services.AddHostedService<LeaveAccrualHostedService>();
builder.Services.AddHostedService<Codeji.CMS.Services.CareerPortal.CareerNotificationWorker>();
builder.Services.AddHostedService<Codeji.CMS.Services.Recruitments.RecruitmentEmailOutboxWorker>();
builder.Services.AddHostedService<Codeji.CMS.Services.Recruitments.RecruitmentRetentionWorker>();
builder.Services.AddHostedService<Codeji.CMS.Services.Attendance.AttendanceOutboxWorker>();
builder.Services.AddHostedService<Codeji.CMS.Services.Attendance.WorkFromHomeOutboxWorker>();
builder.Services.AddHostedService<Codeji.CMS.Services.Attendance.AttendanceReminderHostedService>();
builder.Services.AddHostedService<EmployeeExitOutboxWorker>();
builder.Services.AddHostedService<Codeji.CMS.Services.Attendance.LeaveAttendanceReconciliationHostedService>();
builder.Services.AddHttpContextAccessor();
// MongoDB Configuration
ConfigurationManager configuration = builder.Configuration;
var mongoConnection = configuration.GetConnectionString("mongodb")
    ?? throw new Exception("ConnectionStrings:mongodb is not configured.");
var mongoUrl = MongoUrl.Create(mongoConnection);
if (string.IsNullOrEmpty(mongoUrl.DatabaseName))
    throw new Exception("ConnectionStrings:mongodb must include the database name in the path.");

builder.Services.AddSingleton<IMongoClient>(_ => new MongoDB.Driver.MongoClient(mongoConnection));
builder.Services.AddScoped<IMongoDatabase>(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(mongoUrl.DatabaseName));


ConfigManager.Initialize(builder.Configuration);


builder.Services.AddBusinessServices();

builder.Services.AddHttpClient();

builder.Services.AddRepositoryServices();

builder.Services.AddSingleton<IAuthorizationHandler, RoleHandler>();
var mapsterConfig = TypeAdapterConfig.GlobalSettings;
mapsterConfig.Scan(typeof(MapsterConfig).Assembly);
builder.Services.AddSingleton(mapsterConfig);
builder.Services.AddScoped<MapsterMapper.IMapper, ServiceMapper>();


builder.Services.AddTransient<PdfService>();

// Authentication and Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = ConfigManager.AppSettings.APIUrl,
            ValidAudience = ConfigManager.AppSettings.AppUrl,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ConfigManager.Jwt.SecretKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var principal = context.Principal;
                var subject = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userId = principal?.FindFirst("user_id")?.Value;
                var companyId = principal?.FindFirst("company_id")?.Value;
                var roleId = principal?.FindFirst("role_id")?.Value;
                if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(userId) ||
                    !string.Equals(subject, userId, StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(roleId))
                    context.Fail("Required tenant identity claims are missing or inconsistent.");
                return Task.CompletedTask;
            },            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                // If the request is for our hub...
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/notificationhub") || path.StartsWithSegments("/chathub")))
                {
                    // Read the token out of the query string
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// register policy with authorization service
builder.Services.AddAuthorization(option =>
{
    option.AddPolicy("AdminOnly", policy =>
    {
        policy.Requirements.Add(new RoleRequirement(EnumsHelper.Roles.Administrator));
    });
    option.AddPolicy("HrExitOverride", policy => policy.Requirements.Add(new AnyRoleRequirement(EnumsHelper.Roles.Administrator, EnumsHelper.Roles.HR, EnumsHelper.Roles.HRExecutive)));
});
builder.Services.AddSingleton<IAuthorizationHandler, AnyRoleHandler>();

// Antiforgery
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "XSRF-TOKEN";
    options.Cookie.Name = "X_CSRFTken";
});

// SignalR Configuration
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 64 * 1024;
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// to track users presence in application 
builder.Services.AddSingleton<IEmployeePresenceService, EmployeePresenceService>();


// mogodb repositories
builder.Services.AddScoped<IMongoDbRepository<EmpUser>, MongoRepository<EmpUser>>();
builder.Services.AddScoped<IMongoDbRepository<EmployeeLifecyclePolicy>, MongoRepository<EmployeeLifecyclePolicy>>();
builder.Services.AddScoped<IMongoDbRepository<EmployeeLifecycleCase>, MongoRepository<EmployeeLifecycleCase>>();
builder.Services.AddScoped<IEmployeeLifecycleCaseService, EmployeeLifecycleCaseService>();
builder.Services.AddScoped<IEmployeeLifecyclePolicyService, EmployeeLifecyclePolicyService>();
builder.Services.AddScoped<IMongoDbRepository<EmpPayRoll>, MongoRepository<EmpPayRoll>>();
builder.Services.AddScoped<IMongoDbRepository<PayrollRun>, MongoRepository<PayrollRun>>();
builder.Services.AddScoped<IMongoDbRepository<PayrollRunEmployee>, MongoRepository<PayrollRunEmployee>>();
builder.Services.AddScoped<IMongoDbRepository<PayrollPaymentBatch>, MongoRepository<PayrollPaymentBatch>>();
builder.Services.AddScoped<IMongoDbRepository<PayrollAdjustment>, MongoRepository<PayrollAdjustment>>();
builder.Services.AddScoped<IMongoDbRepository<EmployeeTaxDeclaration>, MongoRepository<EmployeeTaxDeclaration>>();
builder.Services.AddScoped<IMongoDbRepository<PayrollPolicyProfile>, MongoRepository<PayrollPolicyProfile>>();
builder.Services.AddScoped<IMongoDbRepository<SalaryTemplate>, MongoRepository<SalaryTemplate>>();
builder.Services.AddScoped<IMongoDbRepository<PayrollPayGroup>, MongoRepository<PayrollPayGroup>>();
builder.Services.AddScoped<IMongoDbRepository<PayrollSalaryComponentDefinition>, MongoRepository<PayrollSalaryComponentDefinition>>();
builder.Services.AddScoped<IMongoDbRepository<EmployeePayrollAssignment>, MongoRepository<EmployeePayrollAssignment>>();
builder.Services.AddScoped<IMongoDbRepository<EmployeeSalaryStructure>, MongoRepository<EmployeeSalaryStructure>>();
builder.Services.AddScoped<IMongoDbRepository<EmployeeSalaryImportBatch>, MongoRepository<EmployeeSalaryImportBatch>>();
builder.Services.AddScoped<IMongoDbRepository<PayrollDivisorPolicy>, MongoRepository<PayrollDivisorPolicy>>();
builder.Services.AddScoped<IMongoDbRepository<MonthlyAttendanceSummary>, MongoRepository<MonthlyAttendanceSummary>>();
builder.Services.AddScoped<IMongoDbRepository<Codeji.CMS.Repository.Entities.Company.Company>, MongoRepository<Codeji.CMS.Repository.Entities.Company.Company>>();
builder.Services.AddScoped<IMongoDbRepository<Codeji.CMS.Repository.Entities.Company.Department>, MongoRepository<Codeji.CMS.Repository.Entities.Company.Department>>();
builder.Services.AddScoped<IMongoDbRepository<Codeji.CMS.Repository.Entities.Company.JobTitles>, MongoRepository<Codeji.CMS.Repository.Entities.Company.JobTitles>>();

builder.Services.AddScoped<IMongoDbRepository<RefreshToken>, MongoRepository<RefreshToken>>();
builder.Services.AddScoped<IMongoDbRepository<EmployeeExit>, MongoRepository<EmployeeExit>>();
builder.Services.AddScoped<IMongoDbRepository<EmployeeExitPolicy>, MongoRepository<EmployeeExitPolicy>>();
builder.Services.AddScoped<IMongoDbRepository<EmployeeExitClearanceTemplate>, MongoRepository<EmployeeExitClearanceTemplate>>();
builder.Services.AddScoped<IMongoDbRepository<EmployeeFinalSettlement>, MongoRepository<EmployeeFinalSettlement>>();
builder.Services.AddScoped<IMongoDbRepository<EmployeeExitAttendanceCutoff>, MongoRepository<EmployeeExitAttendanceCutoff>>();
builder.Services.AddScoped<IMongoDbRepository<EmployeeExitOutbox>, MongoRepository<EmployeeExitOutbox>>();
builder.Services.AddScoped<IMongoDbRepository<FormerEmployeeAccessToken>, MongoRepository<FormerEmployeeAccessToken>>();
builder.Services.AddSingleton<IEmployeeExitEvidenceStorage, PrivateEmployeeExitEvidenceStorage>();
builder.Services.AddScoped<IFormerEmployeePortalService, FormerEmployeePortalService>();
builder.Services.AddScoped<IMongoDbRepository<LeaveRequest>, MongoRepository<LeaveRequest>>();
builder.Services.AddScoped<IMongoDbRepository<WorkFromHomeRequest>, MongoRepository<WorkFromHomeRequest>>();
// salary 
builder.Services.AddScoped<MongoDbContext>();
builder.Services.AddScoped<ISalaryRepository, SalaryRepository>();
builder.Services.AddScoped<ISalaryService, SalaryService>();


// Payroll Services
builder.Services.AddScoped<IPayRollServices, PayRollServices>();
builder.Services.AddSingleton<IProfessionalTaxResolver, ProfessionalTaxResolver>();
builder.Services.AddScoped<ITaxDeclarationService, TaxDeclarationService>();
builder.Services.AddScoped<IPayrollReadinessService, PayrollReadinessService>();
builder.Services.AddScoped<IPayrollTemplateService, PayrollTemplateService>();
builder.Services.AddScoped<IPayrollConfigurationService, PayrollConfigurationService>();
builder.Services.AddScoped<IEmployeeSalaryStructureService, EmployeeSalaryStructureService>();
builder.Services.AddScoped<IEmployeeSalaryImportService, EmployeeSalaryImportService>();
builder.Services.AddScoped<IPayrollRunService, PayrollRunService>();
builder.Services.AddScoped<IPayrollRunWorkflowService, PayrollRunWorkflowService>();
builder.Services.AddScoped<IPayrollRunCalculationService, PayrollRunCalculationService>();
builder.Services.AddScoped<IPayrollRunReviewService, PayrollRunReviewService>();

//attendance 
// builder.Services.AddScoped<IAttendanceService, IAttendanceService>();
// Register Attendance Repository
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();

// Register Service Layer
builder.Services.AddScoped<IAdminAttendanceService, AdminAttendanceService>();

// builder.Services.AddScoped<IAttendanceService, IAttendanceService>();
// Auto Payroll Services
builder.Services.AddScoped<IAutoPayRollServices, AutoPayrollServices>();

// Hosted Services
builder.Services.AddHostedService<PayrollHostedService>();

//company Services 
builder.Services.AddScoped<ICompanyService, CompanyService>();

// Configure IIS Integration
builder.WebHost.UseIISIntegration();
builder.Logging.AddConsole();
// Build the application
WebApplication app = builder.Build();

// Middleware pipeline
app.UseMiddleware(typeof(ExceptionHandlingMiddleware));

// Enable Swagger and Swagger UI
// Configure the HTTP request pipeline
if (Convert.ToBoolean(configuration.GetSection("AppSettings:isForDebug").Value))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/V2/swagger.json", "Codeji Backend API"); });

}



// ensure Uploads folder exists before serving static files 
string uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// CORS first â€” UseStaticFiles is terminal middleware, so the CORS middleware
// never gets a chance to attach headers to /fs/* responses. We move UseCors
// above static files for canonical ordering and ALSO attach CORS headers
// directly via OnPrepareResponse below â€” that's the only path that actually
// works for static-file responses.
app.UseCors(corsName);

// Serve static files
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = new PathString("/fs"),
    OnPrepareResponse = ctx =>
    {
        // Mirror the allow-list from the corsName CORS policy declared at the
        // top of this file. Keep the two predicates in sync.
        var origin = ctx.Context.Request.Headers.Origin.ToString();
        if (string.IsNullOrEmpty(origin)) return;
        try
        {
            var host = new Uri(origin).Host;
            if (host == "localhost" || host.EndsWith(".codeji.in"))
            {
                ctx.Context.Response.Headers["Access-Control-Allow-Origin"] = origin;
                ctx.Context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
                ctx.Context.Response.Headers["Vary"] = "Origin";
            }
        }
        catch (UriFormatException)
        {
            // Malformed Origin header â€” silently skip, response gets no CORS
            // headers and the browser blocks (correct behavior).
        }
    }
});
// Local development profiles may intentionally expose HTTP only. Avoid emitting an
// unusable HTTPS redirect when no development HTTPS endpoint is configured.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
// This must precede rate limiting and authentication so both use the client IP
// supplied by a configured trusted proxy.
app.UseForwardedHeaders();
app.UseRateLimiter();
app.UseAuthentication();
// Establish the authenticated tenant before any policy handler performs a
// tenant-scoped repository lookup.  In particular, AdminOnly verifies the
// role against CompanyId, so it must run after this request validation.
app.UseMiddleware<AntiforgeryMiddleware>();
app.UseAuthorization();
// app.UseMiddleware<CompanyIdMiddleware>();

// Configure SignalR hub
app.MapHub<NotificationHub>("/notificationhub", options =>
{
    options.Transports = HttpTransportType.WebSockets;
    options.ApplicationMaxBufferSize = 6000000;
    options.TransportMaxBufferSize = 6000000;
});

app.MapHub<ChatHub>("/chathub", options =>
{
    options.Transports = HttpTransportType.WebSockets;
    options.ApplicationMaxBufferSize = 6000000;
    options.TransportMaxBufferSize = 6000000;
});

// Configure controller routes
app.MapControllers();
// Add security headers.
// Use indexer assignment (not .Add) so duplicate keys don't throw â€” .NET 10's
// IHeaderDictionary.Add throws ArgumentException on existing keys (ASP0019).
app.Use(async (context, next) =>
{
    var h = context.Response.Headers;
    h["Strict-Transport-Security"]     = "max-age=31536000; includeSubDomains; preload;";
    h["referrer-policy"]               = "same-origin";
    h["x-content-type-options"]        = "nosniff";
    h["x-frame-options"]               = "DENY";
    h["X-Permitted-Cross-Domain-Policies"] = "none";
    h["x-xss-protection"]              = "1; mode=block";
    h["Content-Security-Policy"]       = "default-src 'self';";
    h.Remove("Server");
    h.Remove("x-powered-by");
    h.Remove("X-Powered-By");
    await next();
});


// Run the application
app.Run();



