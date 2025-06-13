using System.Net;
using System.Text;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.API.Notification;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.GenericRepository.Registration;
using Codeji.CMS.GenericRepository.Settings;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Registration;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

string corsName = "codeji";
builder.Services.AddCors(option => option.AddPolicy(corsName, builder =>
{
    builder.AllowCredentials().WithOrigins("http://127.0.0.1:5173", "http://localhost:5173").AllowAnyHeader().AllowAnyMethod();
    // builder.AllowCredentials().AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
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
    options.ForwardLimit = 0;
    options.KnownProxies.Add(IPAddress.Parse("::ffff:192.165.10.20"));
    options.ForwardedForHeaderName = "X-Forwarded-For-You";
});

// SignalR and Notification Services
builder.Services.AddSingleton<IUserIdProvider, GetUserIdProvider>();
builder.Services.AddSingleton<NotificationHub>();
builder.Services.AddTransient<INotificationService, NotificationService>();
builder.Services.AddSingleton<AntiforgeryMiddleware>();
builder.Services.AddSingleton<IPriorityTaskQueue, PriorityTaskQueue>();
builder.Services.AddHostedService<PriorityQueuedHostedService>();
builder.Services.AddHttpContextAccessor();

// MongoDB Configuration
ConfigurationManager configuration = builder.Configuration;
builder.Services.Configure<List<MongoDbSettings>>(configuration.GetSection("MongoDbSettings"));
//builder.Services.Configure<AppConfiguration>(configuration.GetSection("AppConfiguration"));
AppConfiguration? appConfigurations = configuration.GetSection("AppConfiguration").Get<AppConfiguration>();
ConfigManager.Initialize(appConfigurations);

builder.Services.AddSingleton(appConfigurations); // Optional, if needed elsewhere
// Register business logic services
builder.Services.AddBusinessServices();

// Register HTTP client service
builder.Services.AddHttpClient();

// Register repository services
builder.Services.AddRepositoryServices();

// Initialize configuration helper
ConfigurationHelper.Initialize(configuration);
//automapper

builder.Services.AddAutoMapper(typeof(AutoMapperObjects));
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                // If the request is for our hub...
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationhub"))
                {
                    // Read the token out of the query string
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
// Antiforgery
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "XSRF-TOKEN";
    options.Cookie.Name = "X_CSRFTken";
});

// SignalR Configuration
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = null;
    options.EnableDetailedErrors = true;
});

// Configure IIS Integration
builder.WebHost.UseIISIntegration();
builder.Logging.AddConsole();
// Build the application
WebApplication app = builder.Build();

// Middleware pipeline
app.UseMiddleware(typeof(ExceptionHandlingMiddleware));

// Enable Swagger and Swagger UI
// Configure the HTTP request pipeline
if (Convert.ToBoolean(configuration.GetSection("AppConfiguration:AppSettings:isForDebug").Value))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/V2/swagger.json", "Codeji Backend API"); });

}

// CORS configuration
app.UseCors(corsName);

// Serve static files
//app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Uploads")),
    RequestPath = new PathString("/fs")
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AntiforgeryMiddleware>();
// app.UseMiddleware<CompanyIdMiddleware>();

// Configure SignalR hub
app.MapHub<NotificationHub>("/notificationhub", options =>
{
    options.Transports = HttpTransportType.WebSockets;
    options.ApplicationMaxBufferSize = 6000000;
    options.TransportMaxBufferSize = 6000000;
});

// app.MapHub<NoticeBoardHub>("/notice-board");

// Configure controller routes
app.MapControllers();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost
});
// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload;");
    context.Response.Headers.Add("referrer-policy", new StringValues("same-origin"));
    context.Response.Headers.Add("x-content-type-options", new StringValues("nosniff"));
    context.Response.Headers.Add("x-frame-options", new StringValues("DENY"));
    context.Response.Headers.Add("X-Permitted-Cross-Domain-Policies", new StringValues("none"));
    context.Response.Headers.Add("x-xss-protection", new StringValues("1; mode=block"));
    context.Response.Headers.Add("Content-Security-Policy", new StringValues("default-src 'self';"));
    if (context.Response.Headers.ContainsKey("Server"))
    {
        context.Response.Headers.Remove("Server");
    }
    if (context.Response.Headers.ContainsKey("x-powered-by") || context.Response.Headers.ContainsKey("X-Powered-By"))
    {
        context.Response.Headers.Remove("x-powered-by");
        context.Response.Headers.Remove("X-Powered-By");
    }
    await next();
});

// Run the application
app.Run();
