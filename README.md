# car-repair-os-service

Microservice extracted from `car-repair-app` for the Customer, Vehicle, ServiceOrder, ServiceOrderItem and ServiceStatusHistory domains.

## Architecture

- Clean Architecture with `Api`, `Application`, `Domain` and `Infrastructure`
- CQRS with MediatR handlers
- FluentValidation pipeline behavior
- AutoMapper profiles
- PostgreSQL with EF Core mappings and DbContext
- RabbitMQ integration with topic exchange publisher/consumer abstractions, DLQ and retry support

## Scope kept from the original project

- Customer management
- Vehicle management
- Service order intake and lifecycle
- Service order items
- Service status history

## Scope intentionally removed

- Payment logic
- Budget / approval logic
- Service execution / job logic

## Running locally

1. Configure `ConnectionStrings__DefaultConnection` or the `Database__*` environment variables.
2. Optionally configure `RabbitMq__*` environment variables for broker connectivity.
3. Run `dotnet ef database update --project src/Infrastructure/Infrastructure.csproj --startup-project src/Api/Api.csproj`.
4. Run `dotnet build CarRepairOsService.sln`.
5. Run the API project with `dotnet run --project src/Api/Api.csproj`.

## Environment variables

- `ConnectionStrings__DefaultConnection`: full PostgreSQL connection string override.
- `Database__Host`, `Database__Port`, `Database__Database`, `Database__Username`, `Database__Password`, `Database__IncludeErrorDetail`: discrete database settings used when the connection string is absent.
- `RabbitMq__HostName`, `RabbitMq__Port`, `RabbitMq__UserName`, `RabbitMq__Password`, `RabbitMq__VirtualHost`, `RabbitMq__ExchangeName`, `RabbitMq__MaxRetries`, `RabbitMq__RetryDelayMilliseconds`: RabbitMQ settings.
- `HttpsRedirection__Enabled`: enables HTTPS redirection when the runtime environment terminates TLS.

## CI / SonarCloud required GitHub Secrets

Configure the following repository secrets to enable the SonarCloud stage in `.github/workflows/ci.yml`:

- `SONAR_TOKEN`: SonarCloud token with permission to analyze the project.
- `SONAR_PROJECT_KEY`: SonarCloud project key.
- `SONAR_ORGANIZATION`: SonarCloud organization key.

## CD / GHCR secrets and permissions

The CD workflow in `.github/workflows/cd.yml` publishes Docker images to GHCR using the default `GITHUB_TOKEN`.

- No additional repository secret is required for GHCR publish in the same repository.
- Ensure workflow permissions allow `packages: write` (already configured in `cd.yml`).
