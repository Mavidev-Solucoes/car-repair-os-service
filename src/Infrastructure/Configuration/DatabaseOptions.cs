using Npgsql;

namespace Infrastructure.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5432;
    public string Database { get; init; } = "car_repair_os";
    public string Username { get; init; } = "postgres";
    public string Password { get; init; } = "postgres";
    public bool IncludeErrorDetail { get; init; }
    public string? SearchPath { get; init; }

    public string BuildConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = Host,
            Port = Port,
            Database = Database,
            Username = Username,
            Password = Password,
            IncludeErrorDetail = IncludeErrorDetail
        };

        if (!string.IsNullOrWhiteSpace(SearchPath))
        {
            builder.SearchPath = SearchPath;
        }

        return builder.ConnectionString;
    }
}
