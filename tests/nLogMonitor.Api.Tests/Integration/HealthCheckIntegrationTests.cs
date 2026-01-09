using System.Net;
using NUnit.Framework;

namespace nLogMonitor.Api.Tests.Integration;

[TestFixture]
public class HealthCheckIntegrationTests : WebApplicationTestBase
{
    [Test]
    public async Task HealthCheck_Returns200()
    {
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Does.Contain("healthy"));
    }

    [Test]
    public async Task HealthCheck_ReturnsTimestamp()
    {
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Does.Contain("timestamp"));
    }

    [Test]
    public async Task HealthCheck_ReturnsJsonContentType()
    {
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
    }
}
