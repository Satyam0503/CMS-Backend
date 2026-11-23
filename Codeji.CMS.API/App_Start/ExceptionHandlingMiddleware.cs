using System.Runtime.ExceptionServices;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.Utility.Exceptions;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;

namespace Codeji.CMS.API.App_Start
{
    /// <summary>
    /// Global exception handler. Anything thrown out of an endpoint reaches here.
    ///
    /// Handling rules:
    ///   - <see cref="AppException"/> (NotFoundException, ValidationException, etc.)
    ///       → translate to its declared HTTP status; safe to expose its Message.
    ///   - Well-known framework exceptions (UnauthorizedAccessException, KeyNotFoundException,
    ///     ArgumentException, TimeoutException) are mapped to their conventional HTTP codes.
    ///   - <see cref="AntiforgeryValidationException"/> keeps its existing contract:
    ///     HTTP 200 with body { StatusCode: 512, Message }.
    ///   - Anything else is logged and returned as 500 with a generic message
    ///     (in IsForDebug mode, we re-throw so the developer page shows the stack).
    /// </summary>
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
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // The caller has gone away; do not turn a normal HTTP cancellation into a 500 response.
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            // In dev mode, let unexpected exceptions bubble up to the developer page so the
            // full stack is visible. AppException + AntiforgeryValidationException are
            // intentional flow-control, so we still translate those normally.
            // Unit-level middleware tests (and a few host edge cases) may not attach
            // a request service provider. Fall back to the process setting in that case.
            var configuration = context.RequestServices?.GetService<IConfiguration>();
            bool isForDebug = bool.TryParse(configuration?["AppSettings:isForDebug"], out var configuredDebugMode)
                ? configuredDebugMode
                : ConfigManager.AppSettings?.IsForDebug ?? false;
            if (isForDebug && ex is not AppException && ex is not AntiforgeryValidationException)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
                return;
            }

            var (statusCode, message) = ResolveStatus(ex);
            var correlationId = context.TraceIdentifier;

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                TryLogError(ex, statusCode, context, correlationId);
            }
            else
            {
                TryLogWarning(ex, statusCode, context, message, correlationId);
            }

            context.Response.ContentType = "application/json";
            context.Response.Headers["X-Correlation-ID"] = correlationId;

            // Antiforgery: keep existing contract — HTTP 200 with body StatusCode 512.
            context.Response.StatusCode = ex is AntiforgeryValidationException
                ? StatusCodes.Status200OK
                : statusCode;

            var result = new Result
            {
                Success = false,
                StatusCode = statusCode,
                Message = message,
            };

            await context.Response.WriteAsync(JsonConvert.SerializeObject(result));
        }

        // A broken logging provider must not prevent the error response from reaching the
        // caller (for example, Windows Event Log may be unavailable to a service account).
        private void TryLogError(Exception ex, int statusCode, HttpContext context, string correlationId)
        {
            try
            {
                _logger.LogError(ex,
                    "Unhandled exception (status {Status}) on {Method} {Path}. CorrelationId: {CorrelationId}",
                    statusCode, context.Request.Method, context.Request.Path, correlationId);
            }
            catch
            {
                // Preserve the safe HTTP response; operational logging health is monitored separately.
            }
        }

        private void TryLogWarning(Exception ex, int statusCode, HttpContext context, string message, string correlationId)
        {
            try
            {
                _logger.LogWarning(ex,
                    "Handled exception (status {Status}) on {Method} {Path}: {Message}. CorrelationId: {CorrelationId}",
                    statusCode, context.Request.Method, context.Request.Path, message, correlationId);
            }
            catch
            {
                // Preserve the safe HTTP response; operational logging health is monitored separately.
            }
        }

        private static (int StatusCode, string Message) ResolveStatus(Exception ex) => ex switch
        {
            AppException appEx                 => (appEx.StatusCode, appEx.Message),
            AntiforgeryValidationException     => (512, ex.Message),
            UnauthorizedAccessException      => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            KeyNotFoundException               => (StatusCodes.Status404NotFound, "Resource not found"),
            ArgumentException                  => (StatusCodes.Status400BadRequest, ex.Message),
            TimeoutException                   => (StatusCodes.Status504GatewayTimeout, "Request timed out"),
            _                                  => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
        };
    }
}
