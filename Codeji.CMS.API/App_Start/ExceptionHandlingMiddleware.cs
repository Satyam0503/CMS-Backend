using Codeji.CMS.Domain.Models;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Antiforgery;
using Newtonsoft.Json;
using System.Net;
using Task = System.Threading.Tasks.Task;

namespace Codeji.CMS.API.App_Start
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        //private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));

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
            bool isForDebug = Convert.ToBoolean(ConfigManager.Is_For_Debug);
            if (isForDebug)
            {
                throw ex;
            }
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
