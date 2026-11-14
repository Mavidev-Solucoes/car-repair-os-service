using System.Net;
using System.Net.Http.Json;
using Application.DTOs;

namespace Api.IntegrationTests;

[Collection(OsCollection.Name)]
public sealed class OsApiTests(OsFixture fixture)
{
    public sealed record PagedResponse<T>(List<T> Items, int TotalCount, int Page, int PageSize, int TotalPages);

    [Fact]
    public async Task Customers_ShouldSupportCrudFiltersAndRules()
    {
        using var client = fixture.CreateClient();
        var customer = await OsFixture.CreateCustomerAsync(client, "Ana Pereira");

        var byId = await client.GetFromJsonAsync<CustomerDto>($"/api/customers/{customer.Id}");
        var filtered = await client.GetFromJsonAsync<PagedResponse<CustomerDto>>(
            $"/api/customers?name=Ana&email={customer.Email}&personalId={customer.PersonalId}&telephone=98888&orderBy=Name&orderDescending=true&pageSize=5");
        Assert.Equal(customer.Email, byId!.Email);
        Assert.Contains(filtered!.Items, item => item.Id == customer.Id);
        Assert.Equal(5, filtered.PageSize);

        var duplicated = await client.PostAsJsonAsync("/api/customers", new
        {
            name = "Outra Pessoa",
            personalId = customer.PersonalId,
            email = "outra@example.com",
            telephone = "11988887777"
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, duplicated.StatusCode);

        var duplicatedEmail = await client.PostAsJsonAsync("/api/customers", new
        {
            name = "Outra Pessoa",
            personalId = "98765432100",
            email = customer.Email,
            telephone = "11988887777"
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, duplicatedEmail.StatusCode);

        var invalid = await client.PostAsJsonAsync("/api/customers", new { name = "Ana", personalId = "1", email = "x", telephone = "1" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var updated = await client.PutAsJsonAsync($"/api/customers/{customer.Id}", new
        {
            id = customer.Id,
            name = "Ana Pereira Souza",
            email = $"novo.{Guid.NewGuid():N}@example.com",
            telephone = "1133334444",
            isActive = false
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var updatedDto = await updated.Content.ReadFromJsonAsync<CustomerDto>();
        Assert.Equal("Ana Pereira Souza", updatedDto!.Name);
        Assert.False(updatedDto.IsActive);

        var other = await OsFixture.CreateCustomerAsync(client);
        var emailTaken = await client.PutAsJsonAsync($"/api/customers/{customer.Id}", new
        {
            id = customer.Id,
            name = "Ana Pereira",
            email = other.Email,
            telephone = "1133334444",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, emailTaken.StatusCode);

        var mismatch = await client.PutAsJsonAsync($"/api/customers/{Guid.NewGuid()}", new
        {
            id = customer.Id,
            name = "Ana Pereira",
            email = "a@example.com",
            telephone = "1133334444",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, mismatch.StatusCode);

        var vehicle = await OsFixture.CreateVehicleAsync(client, other.Id);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.DeleteAsync($"/api/customers/{other.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/vehicles/{vehicle.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/customers/{other.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/customers/{other.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/customers/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Vehicles_ShouldSupportCrudFiltersAndRules()
    {
        using var client = fixture.CreateClient();
        var customer = await OsFixture.CreateCustomerAsync(client);
        var vehicle = await OsFixture.CreateVehicleAsync(client, customer.Id);

        var byId = await client.GetFromJsonAsync<VehicleDto>($"/api/vehicles/{vehicle.Id}");
        var byCustomer = await client.GetFromJsonAsync<List<VehicleDto>>($"/api/vehicles/customer/{customer.Id}");
        var filtered = await client.GetFromJsonAsync<PagedResponse<VehicleDto>>(
            $"/api/vehicles?brand=Fiat&model=Argo&year=2022&licensePlate={vehicle.LicensePlate}&orderBy=Year&orderDescending=true");
        Assert.Equal(vehicle.LicensePlate, byId!.LicensePlate);
        Assert.Single(byCustomer!);
        Assert.Contains(filtered!.Items, item => item.Id == vehicle.Id);

        var duplicatedPlate = await client.PostAsJsonAsync("/api/vehicles", new
        {
            customerId = customer.Id, brand = "VW", model = "Gol", year = 2015, licensePlate = vehicle.LicensePlate, color = (string?)null
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, duplicatedPlate.StatusCode);

        var unknownCustomer = await client.PostAsJsonAsync("/api/vehicles", new
        {
            customerId = Guid.NewGuid(), brand = "VW", model = "Gol", year = 2015, licensePlate = OsFixture.NewPlate(), color = "Azul"
        });
        Assert.Equal(HttpStatusCode.NotFound, unknownCustomer.StatusCode);

        var invalid = await client.PostAsJsonAsync("/api/vehicles", new
        {
            customerId = customer.Id, brand = "", model = "", year = 1800, licensePlate = "123", color = "Azul"
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var newPlate = OsFixture.NewPlate();
        var updated = await client.PutAsJsonAsync($"/api/vehicles/{vehicle.Id}", new
        {
            id = vehicle.Id, brand = "Fiat", model = "Pulse", year = 2023, licensePlate = newPlate, color = "Preto"
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("Pulse", (await updated.Content.ReadFromJsonAsync<VehicleDto>())!.Model);

        var other = await OsFixture.CreateVehicleAsync(client, customer.Id);
        var plateTaken = await client.PutAsJsonAsync($"/api/vehicles/{other.Id}", new
        {
            id = other.Id, brand = "Fiat", model = "Argo", year = 2022, licensePlate = newPlate, color = "Prata"
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, plateTaken.StatusCode);

        var mismatch = await client.PutAsJsonAsync($"/api/vehicles/{Guid.NewGuid()}", new
        {
            id = vehicle.Id, brand = "Fiat", model = "Argo", year = 2022, licensePlate = newPlate, color = "Prata"
        });
        Assert.Equal(HttpStatusCode.BadRequest, mismatch.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/vehicles/{Guid.NewGuid()}")).StatusCode);
        var missingId = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/vehicles/{missingId}", new
        {
            id = missingId, brand = "Fiat", model = "Argo", year = 2022, licensePlate = OsFixture.NewPlate(), color = "Prata"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/vehicles/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task ServiceOrders_ShouldOpenListCancelAndPublishEvents()
    {
        using var probe = new EventsProbe(fixture);
        var userId = Guid.NewGuid();
        using var client = fixture.CreateClient(userId);
        var customer = await OsFixture.CreateCustomerAsync(client, "Carlos Lima");
        var vehicle = await OsFixture.CreateVehicleAsync(client, customer.Id);

        var create = await client.PostAsJsonAsync("/api/service-orders", new
        {
            vehicleId = vehicle.Id,
            customerId = customer.Id,
            items = new[]
            {
                new { serviceItemId = Guid.NewGuid(), description = "Troca de óleo", price = 150m, quantity = 1 },
                new { serviceItemId = Guid.NewGuid(), description = "Filtro", price = 40m, quantity = 2 }
            }
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var order = (await create.Content.ReadFromJsonAsync<ServiceOrderDto>())!;
        Assert.Equal(230m, order.TotalPrice);
        Assert.Equal(userId, order.AssignedUserId);

        var opened = await probe.WaitForAsync("ServiceOrderOpened", order.Id.ToString("D"));
        Assert.Equal("Carlos Lima", opened.Payload!["customerName"]!.GetValue<string>());
        Assert.Equal(2, opened.Payload["items"]!.AsArray().Count);

        var byId = await client.GetFromJsonAsync<ServiceOrderDto>($"/service-orders/{order.Id}");
        var list = await client.GetFromJsonAsync<PagedResponse<ServiceOrderDto>>(
            $"/api/service-orders?customerId={customer.Id}&vehicleId={vehicle.Id}&status={byId!.Status}&orderBy=CreatedAt&orderDescending=true");
        Assert.Equal(2, byId.ServiceItems.Count());
        Assert.Contains(list!.Items, item => item.Id == order.Id);

        var cancel = await client.PatchAsync($"/api/service-orders/{order.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal("Cancelled", (await cancel.Content.ReadFromJsonAsync<ServiceOrderDto>())!.Status);
        await probe.WaitForAsync("ServiceOrderCancelled", order.Id.ToString("D"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.DeleteAsync($"/api/vehicles/{vehicle.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.DeleteAsync($"/api/customers/{customer.Id}")).StatusCode);
    }

    [Fact]
    public async Task ServiceOrders_ShouldRejectInvalidRequests()
    {
        using var client = fixture.CreateClient();
        using var anonymous = fixture.Factory.CreateClient();
        var customer = await OsFixture.CreateCustomerAsync(client);
        var otherCustomer = await OsFixture.CreateCustomerAsync(client);
        var vehicle = await OsFixture.CreateVehicleAsync(client, otherCustomer.Id);

        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync("/api/service-orders", new { vehicleId = vehicle.Id, customerId = customer.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PatchAsync($"/api/service-orders/{Guid.NewGuid()}/cancel", null)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostAsJsonAsync("/api/service-orders", new { vehicleId = vehicle.Id, customerId = customer.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/service-orders", new { vehicleId = vehicle.Id, customerId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/service-orders", new { vehicleId = Guid.NewGuid(), customerId = customer.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/service-orders", new { vehicleId = Guid.Empty, customerId = Guid.Empty })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/service-orders?status=Inexistente")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/service-orders/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsync($"/api/service-orders/{Guid.NewGuid()}/cancel", null)).StatusCode);
    }

    [Fact]
    public async Task Health_ShouldBeHealthyWithRealDependencies()
    {
        using var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"rabbitmq\":{\"status\":\"Healthy\"", await response.Content.ReadAsStringAsync());
    }
}
