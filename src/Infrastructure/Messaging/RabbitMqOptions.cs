namespace Infrastructure.Messaging;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public bool Enabled { get; set; } = true;
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    /// <summary>Exchange onde o serviço publica seus eventos.</summary>
    public string ExchangeName { get; set; } = "car-repair.events";
    /// <summary>Exchange de onde o serviço consome os comandos da saga.</summary>
    public string CommandsExchangeName { get; set; } = "car-repair.commands";
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMilliseconds { get; set; } = 5000;
}
