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

1. Configure `ConnectionStrings:DefaultConnection` via user secrets or environment variables.
2. Run `dotnet build /home/runner/work/car-repair-os-service/car-repair-os-service/CarRepairOsService.sln`.
3. Run the API project with `dotnet run --project /home/runner/work/car-repair-os-service/car-repair-os-service/src/Api/Api.csproj`.
