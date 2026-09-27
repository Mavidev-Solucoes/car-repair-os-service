using Application.Customers.Queries;
using Application.ServiceOrders.Queries;
using Application.Vehicles.Queries;

namespace Domain.UnitTests;

public class QueryValidationTests
{
    [Fact]
    public void GetCustomersQuery_WithInvalidPaging_ShouldFailValidation()
    {
        var validator = new GetCustomersQueryValidator();
        var query = new GetCustomersQuery { Page = 0, PageSize = 0 };

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetCustomersQuery.Page));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetCustomersQuery.PageSize));
    }

    [Fact]
    public void GetVehiclesQuery_WithInvalidPaging_ShouldFailValidation()
    {
        var validator = new GetVehiclesQueryValidator();
        var query = new GetVehiclesQuery { Page = -1, PageSize = 500 };

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetVehiclesQuery.Page));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetVehiclesQuery.PageSize));
    }

    [Fact]
    public void GetServiceOrdersQuery_WithInvalidStatus_ShouldFailValidation()
    {
        var validator = new GetServiceOrdersQueryValidator();
        var query = new GetServiceOrdersQuery { Page = 1, PageSize = 10, Status = "invalid-status" };

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetServiceOrdersQuery.Status));
    }

    [Fact]
    public void GetServiceOrdersQuery_WithValidStatusAndPaging_ShouldPassValidation()
    {
        var validator = new GetServiceOrdersQueryValidator();
        var query = new GetServiceOrdersQuery { Page = 1, PageSize = 20, Status = "Received" };

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }
}
