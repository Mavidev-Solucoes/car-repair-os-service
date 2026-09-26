namespace Api.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _httpClient;

    public HealthEndpointTests(CustomWebApplicationFactory factory)
    {
        _httpClient = factory.CreateClient();
    }

    [Fact(Skip = "Integration scenarios need containerized PostgreSQL and RabbitMQ dependencies.")]
    public async Task HealthEndpoint_ShouldReturnSuccess_WhenDependenciesAreAvailable()
    {
        var response = await _httpClient.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }
}
