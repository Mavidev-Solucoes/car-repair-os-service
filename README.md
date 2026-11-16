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

## Observabilidade (New Relic)

A imagem traz o agente .NET do New Relic com o profiler desligado; no Kubernetes o ConfigMap liga o agente (`NEW_RELIC_APP_NAME=car-repair-os-service`) e a license vem do Secret opcional `newrelic-license`.

O trace distribuído atravessa o RabbitMQ: `MessageTracing` grava `traceparent`/`tracestate` (W3C) e os headers do New Relic em cada mensagem publicada e, no consumo, abre a transação `RabbitMQ/<tipo>` que continua o trace de quem publicou, com o atributo `correlationId` (id da OS). Detalhes e consultas NRQL no README do `car-repair-k8s-infra`, seção "Observabilidade: New Relic APM e trace distribuído".

## Evidências de testes e cobertura — FIAP Fase 4

- **Cobertura de linhas:** **94,06%**, acima do mínimo de **80%** solicitado pela FIAP, medida na execução de CI da branch `main` em **15/11/2026**.
- **Evidência de cobertura e qualidade:** [GitHub Actions CI — build, testes, cobertura, SonarCloud e Docker aprovados](https://github.com/Mavidev-Solucoes/car-repair-os-service/actions/runs/43689845445).
- **Testes automatizados:** `tests/Domain.UnitTests` e `tests/Api.IntegrationTests`.
- **Evidência de integração entre serviços:** [E2E Kubernetes (kind) — fluxo completo e compensações da Saga](https://github.com/Mavidev-Solucoes/car-repair-k8s-infra/actions/runs/43935058397).

O workflow [`.github/workflows/ci.yml`](.github/workflows/ci.yml) aplica o limite de 80% e executa a análise SonarCloud quando disponível. O percentual acima é o resultado da execução vinculada, não uma garantia para futuros commits.

## Papel na Saga da ordem de serviço

Este microsserviço é responsável pela abertura, histórico e estado da OS, com persistência própria em PostgreSQL. Ao abrir uma OS, publica `ServiceOrderOpened` no RabbitMQ. O [orquestrador](https://github.com/Mavidev-Solucoes/car-repair-saga-orchestrator) coordena orçamento, pagamento e execução; ao final, o OS Service consome `CloseServiceOrderCommand`, ou `CancelServiceOrderCommand` se houver compensação, atualizando o estado local.

A divisão de responsabilidades, os eventos, comandos e a correlação por ID da OS estão descritos no [contrato de mensageria](docs/contrato-mensageria.md). O serviço não consulta diretamente os bancos dos demais microsserviços.

## Entrega e API

- [Dockerfile](src/Api/Dockerfile), [manifests Kubernetes](k8s/base) e [workflows independentes de CI/CD](.github/workflows) acompanham o código.
- A API disponibiliza Swagger/OpenAPI em ambiente de desenvolvimento.
- A plataforma e os cenários end-to-end são documentados no [repositório de Kubernetes](https://github.com/Mavidev-Solucoes/car-repair-k8s-infra).

