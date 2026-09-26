using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Api.IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("HttpsRedirection:Enabled", "false"),
                new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", string.Empty),
                new KeyValuePair<string, string?>("Database:Host", "127.0.0.1"),
                new KeyValuePair<string, string?>("Database:Port", "15432"),
                new KeyValuePair<string, string?>("Database:Database", "car_repair_os_test"),
                new KeyValuePair<string, string?>("Database:Username", "postgres"),
                new KeyValuePair<string, string?>("Database:Password", "postgres"),
                new KeyValuePair<string, string?>("RabbitMq:Enabled", "true"),
                new KeyValuePair<string, string?>("RabbitMq:HostName", "127.0.0.1"),
                new KeyValuePair<string, string?>("RabbitMq:Port", "15672"),
                new KeyValuePair<string, string?>("RabbitMq:UserName", "guest"),
                new KeyValuePair<string, string?>("RabbitMq:Password", "guest"),
                new KeyValuePair<string, string?>("RabbitMq:VirtualHost", "/"),
                new KeyValuePair<string, string?>("RabbitMq:ExchangeName", "car-repair.events")
            ]);
        });
    }
}
