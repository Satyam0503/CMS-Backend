
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Antiforgery;
using Newtonsoft.Json;
using System.Net;
using System.Text;
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
                bool isForDebug = Convert.ToBoolean(ConfigManager.Is_For_Debug);
                bool isGetRequest = string.Equals("GET", context.Request.Method, StringComparison.OrdinalIgnoreCase);
                IHeaderDictionary headers = context.Request.Headers;
                IHttpContextAccessor _httpContextAccessor = (IHttpContextAccessor)context.RequestServices.GetService(typeof(IHttpContextAccessor));
                string companyId = CurrentContext.CurrentUserCompanyId(_httpContextAccessor);
                string userId = CurrentContext.CurrentUserId(_httpContextAccessor);
                string requestPath = context?.Request.Path.Value ?? "";
                bool isPartner = CurrentContext.IsPartnerAccount(_httpContextAccessor);
                string[] excludedUrls = new string[] { "/notificationhub", "/GetAppVersion", "/antiforgerytoken" };
                if (!isForDebug && !isGetRequest && !context.User.Identity.IsAuthenticated)
                {
                    string requestId = new Guid().ToString();
                    _antiforgery.ValidateRequestAsync(context).GetAwaiter().GetResult();
                }
                else if (context.User.Identity.IsAuthenticated && !isPartner && string.IsNullOrEmpty(companyId) && !excludedUrls.Any(requestPath.Contains))
                {
                    var responseObject = new
                    {
                        StatusCode = HttpStatusCode.Unauthorized
                    };
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(responseObject));
                }
                context.Request.Headers.TryGetValue("AppVersion", out Microsoft.Extensions.Primitives.StringValues appVersion);
                string appVersionStr = appVersion.FirstOrDefault()?.ToString().Trim() ?? "";

                string normalizedRequestPath = requestPath.TrimEnd('/');

                if (!isForDebug && !excludedUrls.Any(requestPath.Contains) && (string.IsNullOrEmpty(appVersionStr) || string.IsNullOrWhiteSpace(appVersionStr) || (!string.IsNullOrEmpty(appVersionStr) && appVersionStr.ToLowerInvariant() != ConfigManager.App_Version.ToLower())))
                {
                    var responseObject = new
                    {
                        StatusCode = HttpStatusCode.UpgradeRequired,
                        Message = "Upgrade Required"
                    };

                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.UpgradeRequired;

                    // Serialize and write JSON response
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(responseObject));
                }
                else
                {
                    await next(context);
                }
            }
            catch (AntiforgeryValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }

        }
    }
}
