using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace Api.IntegrationTests;

[Collection(OsCollection.Name)]
public sealed class SwaggerTests(OsFixture fixture)
{
    [Fact]
    public async Task Swagger_ShouldBeServedOnlyWhenEnabled()
    {
        await using var enabled = fixture.Factory.WithWebHostBuilder(builder => builder.UseSetting("Swagger:Enabled", "true"));
        using var enabledClient = enabled.CreateClient();
        using var disabledClient = fixture.Factory.CreateClient();

        var document = await enabledClient.GetAsync("/swagger/v1/swagger.json");
        var hidden = await disabledClient.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, document.StatusCode);
        Assert.Contains("/api/service-orders", await document.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
    }
}
