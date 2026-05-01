
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
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AntiforgeryMiddleware(IAntiforgery antiforgery, IHttpContextAccessor httpContextAccessor)
        {
            _antiforgery = antiforgery ?? throw new ArgumentNullException(nameof(antiforgery));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var debugModeEnabled = ConfigManager.AppSettings?.IsForDebug ?? false;
            var isHttpGet = string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase);
            var currentCompanyId = CurrentContext.CompanyId(_httpContextAccessor);
            var path = context?.Request?.Path.Value ?? string.Empty;

            if (!debugModeEnabled && !isHttpGet && !context.User.Identity.IsAuthenticated)
            {
                await _antiforgery.ValidateRequestAsync(context);
            }

            // check if app version is present in request and path is not in pathForNOCompanyIdRequired then return unauthorized
            #region "App version check"
            context.Request.Headers.TryGetValue("AppVersion", out var versionFromHeader);
            var incomingAppVersion = versionFromHeader.FirstOrDefault()?.Trim() ?? string.Empty;
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

            var con = await CompanyIdMiddleware.AuthenticateUserRequest(_httpContextAccessor);
            if (con == null)
            {
                return;
            }

            context.Items["CompanyId"] = currentCompanyId;
            await next(con);
        }
    }
}
