using System.Net;
using System.Runtime.ExceptionServices;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;

namespace Codeji.CMS.API.App_Start
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (AntiforgeryValidationException antiEx)
            {
                await HandleExceptionAsync(context, antiEx);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            bool isForDebug = ConfigManager.AppSettings?.IsForDebug ?? false;
            if (isForDebug)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
                return;
            }

            _logger.LogError(ex, "Unhandled exception caught by middleware.");

            context.Response.ContentType = "application/json";

            Result result = new Result
            {
                Success = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = "An unexpected error occurred."
            };

            if (ex is AntiforgeryValidationException)
            {
                result.StatusCode = 512;
                result.Message = ex.Message;
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

            string responseJson = JsonConvert.SerializeObject(result);
            await context.Response.WriteAsync(responseJson);
        }
    }
}
