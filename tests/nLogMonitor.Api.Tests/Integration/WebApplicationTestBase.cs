using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using nLogMonitor.Application.Configuration;

namespace nLogMonitor.Api.Tests.Integration;

/// <summary>
/// Base class for integration tests using WebApplicationFactory.
/// </summary>
public abstract class WebApplicationTestBase : IDisposable
{
    protected WebApplicationFactory<Program> Factory { get; }
    protected HttpClient Client { get; }

    protected WebApplicationTestBase()
    {
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
    }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Test fixture configured for Desktop mode.
/// </summary>
public abstract class DesktopModeTestBase : WebApplicationTestBase
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.Configure<AppSettings>(options =>
        {
            options.Mode = AppMode.Desktop;
        });
    }
}
