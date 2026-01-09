using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using nLogMonitor.Application.Configuration;

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
