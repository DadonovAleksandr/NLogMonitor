using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using nLogMonitor.Application.DTOs;

namespace nLogMonitor.Api.Controllers;

/// <summary>
/// Controller for application information.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InfoController : ControllerBase
{
    private static readonly string AppVersion = GetAppVersion();

    /// <summary>
    /// Retrieves application information.
    /// </summary>
    /// <returns>Application information including version.</returns>
    /// <response code="200">Returns the application information.</response>
    [HttpGet]
    [ProducesResponseType(typeof(AppInfoDto), StatusCodes.Status200OK)]
    public ActionResult<AppInfoDto> GetInfo()
    {
        return Ok(new AppInfoDto
        {
            Version = AppVersion
        });
    }

    private static string GetAppVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;

        if (version == null)
            return "0.0.0";

        // For 3-part version (Major.Minor.Build), omit Revision if it's 0
        // For 4-part version, include all parts
        if (version.Revision == 0)
            return $"{version.Major}.{version.Minor}.{version.Build}";

        return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
