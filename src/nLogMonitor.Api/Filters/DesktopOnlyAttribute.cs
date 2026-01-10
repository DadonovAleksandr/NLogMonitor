using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using nLogMonitor.Api.Models;
using nLogMonitor.Application.Configuration;

namespace nLogMonitor.Api.Filters;

/// <summary>
/// Restricts access to Desktop mode only.
/// Returns 404 Not Found in Web mode for security.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DesktopOnlyAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var appSettings = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<AppSettings>>().Value;

        if (appSettings.Mode != AppMode.Desktop)
        {
            var errorResponse = new ApiErrorResponse
            {
                Error = "NotFound",
                Message = "This endpoint is not available.",
                TraceId = context.HttpContext.TraceIdentifier
            };

            context.Result = new NotFoundObjectResult(errorResponse);
            return;
        }

        await next();
    }
}
