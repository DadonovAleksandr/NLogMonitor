using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using nLogMonitor.Application.Configuration;
using NUnit.Framework;

namespace nLogMonitor.Api.Tests.Integration;

/// <summary>
/// Base class for integration tests using WebApplicationFactory.
/// Provides isolated temporary directories for file operations.
/// </summary>
public abstract class WebApplicationTestBase : IDisposable
{
    protected WebApplicationFactory<Program> Factory { get; }
    protected HttpClient Client { get; }

    /// <summary>
    /// JSON serializer options for response deserialization.
    /// </summary>
    protected static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Isolated temporary directory for uploaded files.
    /// </summary>
    protected string TestTempDirectory { get; }

    /// <summary>
    /// Isolated path for recent logs storage file.
    /// </summary>
    protected string TestRecentLogsPath { get; }

    protected WebApplicationTestBase()
    {
        // Create isolated directories for this test run
        var testRunId = Guid.NewGuid().ToString("N")[..8];
        TestTempDirectory = Path.Combine(Path.GetTempPath(), $"nLogMonitor_test_{testRunId}", "temp");
        TestRecentLogsPath = Path.Combine(Path.GetTempPath(), $"nLogMonitor_test_{testRunId}", "recent.json");

        Directory.CreateDirectory(TestTempDirectory);

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(ConfigureServices);
            });
        Client = Factory.CreateClient();
    }

    /// <summary>
    /// Override to configure services for specific tests.
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Default: Web mode
        services.Configure<AppSettings>(options =>
        {
            options.Mode = AppMode.Web;
        });

        // Isolate file storage to test-specific directories
        services.Configure<FileSettings>(options =>
        {
            options.TempDirectory = TestTempDirectory;
        });

        services.Configure<RecentLogsSettings>(options =>
        {
            options.CustomStoragePath = TestRecentLogsPath;
        });
    }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();

        // Cleanup test directories
        CleanupTestDirectories();

        GC.SuppressFinalize(this);
    }

    private void CleanupTestDirectories()
    {
        try
        {
            // Get parent directory (nLogMonitor_test_{testRunId})
            var parentDir = Path.GetDirectoryName(TestTempDirectory);
            if (!string.IsNullOrEmpty(parentDir) && Directory.Exists(parentDir))
            {
                Directory.Delete(parentDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    #region Error Response Helpers

    /// <summary>
    /// Deserializes and validates an API error response.
    /// </summary>
    /// <param name="response">HTTP response to read from.</param>
    /// <param name="expectedError">Expected error type (e.g., "NotFound", "BadRequest").</param>
    /// <param name="expectedMessageContains">Optional substring that should appear in the error message.</param>
    /// <returns>The deserialized error response for additional assertions.</returns>
    protected async Task<ApiErrorResponseDto> AssertApiErrorAsync(
        HttpResponseMessage response,
        string expectedError,
        string? expectedMessageContains = null)
    {
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponseDto>(DefaultJsonOptions);

        Assert.That(error, Is.Not.Null, "Response should contain ApiErrorResponse");
        Assert.That(error!.Error, Is.EqualTo(expectedError), $"Error type should be '{expectedError}'");
        Assert.That(error.Message, Is.Not.Null.And.Not.Empty, "Error message should not be empty");
        Assert.That(error.TraceId, Is.Not.Null.And.Not.Empty, "TraceId should be present for error tracking");

        if (expectedMessageContains != null)
        {
            Assert.That(error.Message, Does.Contain(expectedMessageContains),
                $"Error message should contain '{expectedMessageContains}'");
        }

        return error;
    }

    #endregion

    #region Common DTOs

    /// <summary>
    /// DTO for deserializing API error responses in tests.
    /// </summary>
    protected class ApiErrorResponseDto
    {
        public string Error { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string TraceId { get; set; } = string.Empty;
    }

    #endregion
}

/// <summary>
/// Test fixture configured for Desktop mode.
/// </summary>
public abstract class DesktopModeTestBase : WebApplicationTestBase
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        // Call base to get isolated directories
        base.ConfigureServices(services);

        // Override mode to Desktop
        services.Configure<AppSettings>(options =>
        {
            options.Mode = AppMode.Desktop;
        });
    }
}
