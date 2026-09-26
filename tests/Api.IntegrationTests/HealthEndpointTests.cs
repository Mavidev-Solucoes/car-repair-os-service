using System.Net.Http.Json;

namespace Api.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _httpClient;

    public HealthEndpointTests(CustomWebApplicationFactory factory)
    {
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturnServiceUnavailable_WhenDependenciesAreUnavailable()
    {
        var response = await _httpClient.GetAsync("/health");
        var payload = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();

        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Unhealthy", payload!.Status);
        Assert.Equal("Unhealthy", payload.Checks["postgresql"].Status);
        Assert.Equal("Healthy", payload.Checks["rabbitmq"].Status);
    }

    public sealed record HealthCheckResponse(string Status, Dictionary<string, HealthCheckEntryResponse> Checks);

    public sealed record HealthCheckEntryResponse(string Status, string? Description);
}
