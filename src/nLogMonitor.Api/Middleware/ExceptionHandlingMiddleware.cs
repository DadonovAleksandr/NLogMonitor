using System.Net;
using System.Text.Json;
using nLogMonitor.Api.Models;
using nLogMonitor.Application.Exceptions;

namespace nLogMonitor.Api.Middleware;

/// <summary>
/// Global exception handling middleware.
/// Catches unhandled exceptions and returns unified error responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;

        var (statusCode, errorType, message) = MapException(exception);

        // Log the exception with trace ID for correlation
        LogException(exception, traceId, statusCode);

        // Build the error response
        var errorResponse = new ApiErrorResponse
        {
            Error = errorType,
            Message = message,
            Details = _environment.IsDevelopment() ? GetExceptionDetails(exception) : null,
            TraceId = traceId
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(errorResponse, JsonOptions);
        await context.Response.WriteAsync(json);
    }

    private static (HttpStatusCode StatusCode, string ErrorType, string Message) MapException(Exception exception)
    {
        return exception switch
        {
            FileNotFoundException ex => (
                HttpStatusCode.NotFound,
                "NotFound",
                $"File not found: {ex.FileName ?? ex.Message}"
            ),

            NoLogFilesFoundException ex => (
                HttpStatusCode.NotFound,
                "NotFound",
                ex.Message
            ),

            ArgumentNullException ex => (
                HttpStatusCode.BadRequest,
                "BadRequest",
                $"Required parameter is missing: {ex.ParamName}"
            ),

            ArgumentException ex => (
                HttpStatusCode.BadRequest,
                "BadRequest",
                ex.Message
            ),

            InvalidOperationException ex => (
                HttpStatusCode.BadRequest,
                "BadRequest",
                ex.Message
            ),

            _ => (
                HttpStatusCode.InternalServerError,
                "InternalServerError",
                "An unexpected error occurred. Please try again later."
            )
        };
    }

    private void LogException(Exception exception, string traceId, HttpStatusCode statusCode)
    {
        var logLevel = statusCode == HttpStatusCode.InternalServerError
            ? LogLevel.Error
            : LogLevel.Warning;

        _logger.Log(
            logLevel,
            exception,
            "Exception occurred. TraceId: {TraceId}, StatusCode: {StatusCode}, Type: {ExceptionType}, Message: {Message}",
            traceId,
            (int)statusCode,
            exception.GetType().Name,
            exception.Message
        );
    }

    private static string GetExceptionDetails(Exception exception)
    {
        var details = new System.Text.StringBuilder();

        details.AppendLine($"Exception Type: {exception.GetType().FullName}");
        details.AppendLine($"Message: {exception.Message}");

        if (exception.InnerException != null)
        {
            details.AppendLine($"Inner Exception: {exception.InnerException.GetType().FullName}");
            details.AppendLine($"Inner Message: {exception.InnerException.Message}");
        }

        details.AppendLine();
        details.AppendLine("Stack Trace:");
        details.AppendLine(exception.StackTrace);

        return details.ToString();
    }
}

/// <summary>
/// Extension methods for registering ExceptionHandlingMiddleware.
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    /// <summary>
    /// Adds the exception handling middleware to the application pipeline.
    /// Should be registered early in the pipeline to catch all exceptions.
    /// </summary>
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
