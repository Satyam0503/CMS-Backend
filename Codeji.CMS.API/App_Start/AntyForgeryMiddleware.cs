
using System.Net;
using System.Text;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Antiforgery;
using Newtonsoft.Json;
using IMiddleware = Microsoft.AspNetCore.Http.IMiddleware;

namespace Codeji.CMS.API.App_Start
{
    public class AntiforgeryMiddleware : IMiddleware
    {
        private readonly IAntiforgery _antiforgery;
        public AntiforgeryMiddleware(IAntiforgery antiforgery)
        {
            _antiforgery = antiforgery;
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                var debugModeEnabled = Convert.ToBoolean(ConfigManager.AppSettings.IsForDebug);
                var isHttpGet = string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase);
                var httpContextAccessor = context.RequestServices.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
                var currentCompanyId = CurrentContext.CompanyId(httpContextAccessor);
                var currentUserId = CurrentContext.UserId(httpContextAccessor);
                var path = context?.Request?.Path.Value ?? string.Empty;
                if (!debugModeEnabled && !isHttpGet && !context.User.Identity.IsAuthenticated)
                {
                    _antiforgery.ValidateRequestAsync(context).GetAwaiter().GetResult();
                }
                // check if app version is present in request and path is not in pathForNOCompanyIdRequired then return unauthorized
                #region "App version check"
                context.Request.Headers.TryGetValue("AppVersion", out var versionFromHeader);
                var incomingAppVersion = versionFromHeader.FirstOrDefault()?.Trim() ?? string.Empty;
                var cleanPath = path.TrimEnd('/');
                var configuredAppVersion = ConfigManager.AppSettings.AppVersion;
                string[] ignoredEndpoints = new string[] { "/notificationhub", "/GetAppVersion", "/antiforgerytoken" };
                if (!debugModeEnabled &&
                    !ignoredEndpoints.Any(path.Contains) &&
                    (string.IsNullOrWhiteSpace(incomingAppVersion) ||
                     !string.Equals(incomingAppVersion, configuredAppVersion, StringComparison.OrdinalIgnoreCase)))
                {
                    var upgradePrompt = new
                    {
                        StatusCode = HttpStatusCode.UpgradeRequired,
                        Message = "Upgrade Required"
                    };

                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.UpgradeRequired;
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(upgradePrompt));
                    return;
                }
                #endregion
                // check if user id is present in request and path is not in pathForNOCompanyIdRequired then return unauthorized
                var con = await CompanyIdMiddleware.AuthenticateUserRequest(httpContextAccessor);
                if (con == null)
                    return;

                context.Items["CompanyId"] = currentCompanyId;
                await next(con);

            }
            catch (AntiforgeryValidationException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
