using System.Net.Http.Json;
using System.Text.Json;
using Application.Common.Messaging;
using Application.DTOs;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Api.IntegrationTests;

/// <summary>
/// Sobe Postgres e RabbitMQ reais (Testcontainers) e a API apontando para eles, com as migrations aplicadas.
/// </summary>
public sealed class OsFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-alpine")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public string RabbitHost => _rabbit.Hostname;

    public int RabbitPort => _rabbit.GetMappedPublicPort(5672);

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbit.StartAsync());

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
            builder.UseSetting("RabbitMq:Enabled", "true");
            builder.UseSetting("RabbitMq:HostName", RabbitHost);
            builder.UseSetting("RabbitMq:Port", RabbitPort.ToString());
            builder.UseSetting("RabbitMq:MaxRetries", "1");
            builder.UseSetting("RabbitMq:RetryDelayMilliseconds", "200");
        });

        // As migrations são aplicadas pelo próprio serviço na subida (DatabaseMigrationHostedService).
        await WaitForAsync(async () =>
        {
            using var scope = Factory.Services.CreateScope();
            var pending = await scope.ServiceProvider.GetRequiredService<CarRepairOsDbContext>().Database.GetPendingMigrationsAsync();
            return pending.Any() ? null : "ok";
        }, TimeSpan.FromSeconds(60));
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _postgres.DisposeAsync();
        await _rabbit.DisposeAsync();
    }

    public HttpClient CreateClient(Guid? userId = null)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", (userId ?? Guid.NewGuid()).ToString());
        return client;
    }

    public RabbitMqOptions RabbitOptions(int maxRetries = 3, int retryDelayMilliseconds = 100) => new()
    {
        Enabled = true,
        HostName = RabbitHost,
        Port = RabbitPort,
        MaxRetries = maxRetries,
        RetryDelayMilliseconds = retryDelayMilliseconds
    };

    public IConnection CreateRawConnection()
        => new ConnectionFactory { HostName = RabbitHost, Port = RabbitPort, UserName = "guest", Password = "guest" }.CreateConnection();

    public static async Task<T> WaitForAsync<T>(Func<Task<T?>> probe, TimeSpan? timeout = null)
        where T : class
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        while (DateTime.UtcNow < deadline)
        {
            var value = await probe();
            if (value is not null)
            {
                return value;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("Condição não foi atingida a tempo.");
    }

    public static async Task<CustomerDto> CreateCustomerAsync(HttpClient client, string name = "Maria Silva")
    {
        var digits = string.Concat(Enumerable.Range(0, 11).Select(_ => Random.Shared.Next(10)));
        var response = await client.PostAsJsonAsync("/api/customers", new
        {
            name,
            personalId = digits,
            email = $"cliente.{Guid.NewGuid():N}@example.com",
            telephone = "(11) 98888-7777"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CustomerDto>())!;
    }

    public static async Task<VehicleDto> CreateVehicleAsync(HttpClient client, Guid customerId)
    {
        var response = await client.PostAsJsonAsync("/api/vehicles", new
        {
            customerId,
            brand = "Fiat",
            model = "Argo",
            year = 2022,
            licensePlate = NewPlate(),
            color = "Prata"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<VehicleDto>())!;
    }

    public static string NewPlate()
    {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        char L() => letters[Random.Shared.Next(letters.Length)];
        int D() => Random.Shared.Next(10);
        return $"{L()}{L()}{L()}{D()}{L()}{D()}{D()}";
    }
}

[CollectionDefinition(Name)]
public sealed class OsCollection : ICollectionFixture<OsFixture>
{
    public const string Name = "os";
}

/// <summary>Fila temporária ligada a car-repair.events para conferir o que o os publicou.</summary>
public sealed class EventsProbe : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _queue;
    private readonly List<IntegrationMessage> _received = [];

    public EventsProbe(OsFixture fixture)
    {
        _connection = fixture.CreateRawConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare("car-repair.events", ExchangeType.Topic, durable: true, autoDelete: false);
        _queue = _channel.QueueDeclare().QueueName;
        _channel.QueueBind(_queue, "car-repair.events", "#");
    }

    public Task<IntegrationMessage> WaitForAsync(string type, string correlationId) => OsFixture.WaitForAsync(() =>
    {
        while (_channel.BasicGet(_queue, true) is { } delivery)
        {
            _received.Add(JsonSerializer.Deserialize<IntegrationMessage>(delivery.Body.Span, IntegrationMessage.SerializerOptions)!);
        }

        return Task.FromResult(_received.FirstOrDefault(message => message.Type == type && message.CorrelationId == correlationId));
    });

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
