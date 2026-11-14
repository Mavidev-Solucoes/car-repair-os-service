using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Common.Messaging;
using Application.DTOs;
using Infrastructure.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Api.IntegrationTests;

[Collection(OsCollection.Name)]
public sealed class OsMessagingTests(OsFixture fixture)
{
    [Fact]
    public async Task SagaCommands_ShouldCloseAndCancelServiceOrders()
    {
        using var probe = new EventsProbe(fixture);
        using var client = fixture.CreateClient();
        await WaitForWorkerAsync(SagaCommandsWorker.CloseServiceOrderCommandType);
        await WaitForWorkerAsync(SagaCommandsWorker.CancelServiceOrderCommandType);
        var toClose = await OpenServiceOrderAsync(client);
        var toCancel = await OpenServiceOrderAsync(client);

        PublishCommand(SagaCommandsWorker.CloseServiceOrderCommandType, toClose.ToString("D"));
        PublishCommand(SagaCommandsWorker.CloseServiceOrderCommandType, toClose.ToString("D"));
        PublishCommand(SagaCommandsWorker.CancelServiceOrderCommandType, toCancel.ToString("D"));

        await probe.WaitForAsync("ServiceOrderClosed", toClose.ToString("D"));
        await probe.WaitForAsync("ServiceOrderCancelled", toCancel.ToString("D"));
        var closed = await client.GetFromJsonAsync<ServiceOrderDto>($"/api/service-orders/{toClose}");
        var cancelled = await client.GetFromJsonAsync<ServiceOrderDto>($"/api/service-orders/{toCancel}");
        Assert.Equal("Closed", closed!.Status);
        Assert.Equal("Cancelled", cancelled!.Status);
    }

    [Fact]
    public async Task SagaCommand_ForUnknownServiceOrder_ShouldRetryThenDeadLetter()
    {
        await WaitForWorkerAsync(SagaCommandsWorker.CancelServiceOrderCommandType);
        var correlationId = Guid.NewGuid().ToString("D");

        PublishCommand(SagaCommandsWorker.CancelServiceOrderCommandType, correlationId);
        PublishRaw(SagaCommandsWorker.CancelServiceOrderCommandType, Encoding.UTF8.GetBytes("{\"type\":\"CancelServiceOrderCommand\",\"correlationId\":\"nao-e-guid\"}"), "guid-invalido");

        await GetFromDlqAsync(SagaCommandsWorker.QueuePrefix + SagaCommandsWorker.CancelServiceOrderCommandType, correlationId, "guid-invalido");
    }

    [Fact]
    public async Task Consumer_ShouldRetryTransientFailuresAndStopOnDispose()
    {
        var commandType = $"FlakyCommand.{Guid.NewGuid():N}";
        var attempts = 0;
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var provider = new RabbitMqConnectionProvider(Options.Create(fixture.RabbitOptions()));
        var consumer = new RabbitMqCommandConsumer(provider, Options.Create(fixture.RabbitOptions()), NullLogger<RabbitMqCommandConsumer>.Instance);

        var subscription = await consumer.StartConsumingAsync($"os-tests.{commandType}", commandType, (_, _) =>
        {
            if (Interlocked.Increment(ref attempts) < 3)
            {
                throw new TimeoutException("instável");
            }

            done.TrySetResult();
            return Task.CompletedTask;
        });

        PublishCommand(commandType, Guid.NewGuid().ToString("D"));
        await done.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(3, attempts);

        await subscription.DisposeAsync();
        await subscription.DisposeAsync();
    }

    [Fact]
    public async Task Consumer_ShouldDeadLetterInvalidAndExhaustedMessages()
    {
        var commandType = $"BrokenCommand.{Guid.NewGuid():N}";
        var queue = $"os-tests.{commandType}";
        var attempts = 0;
        using var provider = new RabbitMqConnectionProvider(Options.Create(fixture.RabbitOptions()));
        var consumer = new RabbitMqCommandConsumer(provider, Options.Create(fixture.RabbitOptions(maxRetries: 1)));

        await using var subscription = await consumer.StartConsumingAsync(queue, commandType, (message, _) =>
        {
            Interlocked.Increment(ref attempts);
            if (message.CorrelationId == "invalida")
            {
                throw new InvalidMessageException("payload inválido");
            }

            throw new InvalidOperationException("falha permanente");
        });

        PublishCommand(commandType, "esgotada");
        PublishCommand(commandType, "invalida");
        PublishRaw(commandType, Encoding.UTF8.GetBytes("não é json"), "json-quebrado");

        await GetFromDlqAsync(queue, "esgotada", "invalida", "json-quebrado");
        Assert.Equal(3, attempts);
    }

    private static async Task<Guid> OpenServiceOrderAsync(HttpClient client)
    {
        var customer = await OsFixture.CreateCustomerAsync(client);
        var vehicle = await OsFixture.CreateVehicleAsync(client, customer.Id);
        var response = await client.PostAsJsonAsync("/api/service-orders", new
        {
            vehicleId = vehicle.Id,
            customerId = customer.Id,
            items = new[] { new { serviceItemId = Guid.NewGuid(), description = "Revisão", price = 300m, quantity = 1 } }
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ServiceOrderDto>())!.Id;
    }

    private void PublishCommand(string type, string correlationId)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(
            new IntegrationMessage { Type = type, CorrelationId = correlationId, Payload = new JsonObject() },
            IntegrationMessage.SerializerOptions);
        PublishRaw(type, body, correlationId);
    }

    private void PublishRaw(string routingKey, byte[] body, string correlationId)
    {
        using var connection = fixture.CreateRawConnection();
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare("car-repair.commands", ExchangeType.Topic, durable: true, autoDelete: false);
        var properties = channel.CreateBasicProperties();
        properties.CorrelationId = correlationId;
        channel.BasicPublish("car-repair.commands", routingKey, properties, body);
    }

    private async Task GetFromDlqAsync(string queue, params string[] correlationIds)
    {
        using var connection = fixture.CreateRawConnection();
        using var channel = connection.CreateModel();
        var seen = new HashSet<string>();
        await OsFixture.WaitForAsync(() =>
        {
            while (channel.BasicGet($"{queue}.dlq", true) is { } delivery)
            {
                seen.Add(delivery.BasicProperties.CorrelationId);
            }

            return Task.FromResult(correlationIds.All(seen.Contains) ? "ok" : null);
        });
    }

    private async Task WaitForWorkerAsync(string commandType)
    {
        using var connection = fixture.CreateRawConnection();
        await OsFixture.WaitForAsync(() =>
        {
            try
            {
                using var channel = connection.CreateModel();
                return Task.FromResult(channel.QueueDeclarePassive(SagaCommandsWorker.QueuePrefix + commandType).ConsumerCount > 0 ? "ok" : null);
            }
            catch
            {
                return Task.FromResult<string?>(null);
            }
        }, TimeSpan.FromSeconds(30));
    }
}
