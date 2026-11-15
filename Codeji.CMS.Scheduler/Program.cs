using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Scheduler;
using Codeji.CMS.Services.Registration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    // Windows services start in System32; resolve our own appsettings.json from the install folder.
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Services.AddWindowsService(options => options.ServiceName = "CodejiCmsScheduler");

// Services resolve Uploads/, Templates/ and evidence folders from the current directory, and payslips
// written here are served by the API at /fs, so jobs must run inside the API's deployed folder.
var apiRoot = builder.Configuration["Scheduler:ApiContentRoot"];
if (string.IsNullOrWhiteSpace(apiRoot) || !File.Exists(Path.Combine(apiRoot, "appsettings.json")))
    throw new InvalidOperationException($"Scheduler:ApiContentRoot must point at the deployed Codeji.CMS.API folder (got '{apiRoot}').");
apiRoot = Path.GetFullPath(apiRoot);
Directory.SetCurrentDirectory(apiRoot);

// The API's appsettings.json is the single source of truth; the scheduler's own settings and env vars override it.
builder.Configuration.Sources.Insert(0, new JsonConfigurationSource
{
    FileProvider = new PhysicalFileProvider(apiRoot),
    Path = "appsettings.json",
    Optional = false,
    ReloadOnChange = true,
});

builder.Services.AddCmsCore(builder.Configuration);
builder.Services.AddCmsBackgroundJobs();

var relayKey = builder.Configuration["InternalApi:Key"];
var apiBaseUrl = builder.Configuration["InternalApi:BaseUrl"];
if (string.IsNullOrWhiteSpace(relayKey) || string.IsNullOrWhiteSpace(apiBaseUrl))
    throw new InvalidOperationException("InternalApi:BaseUrl and InternalApi:Key are required for real-time notifications.");

builder.Services.AddHttpClient<INotificationService, ApiRelayNotificationService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Add("X-Internal-Key", relayKey);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Build().Run();
