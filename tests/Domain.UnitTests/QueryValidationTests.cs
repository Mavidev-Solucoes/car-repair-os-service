using Application.Customers.Queries;
using Application.ServiceOrders.Queries;
using Application.Vehicles.Queries;
using System.Linq.Expressions;
using Application.DTOs;
using Domain.Entities;
using Domain.Interfaces.Repositories;

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

    [Fact]
    public void GetServiceOrdersQuery_WithLowercaseStatus_ShouldPassValidation()
    {
        var validator = new GetServiceOrdersQueryValidator();
        var query = new GetServiceOrdersQuery { Page = 1, PageSize = 20, Status = "diagnosing" };

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void GetServiceOrdersQuery_WithWhitespaceStatus_ShouldPassValidation()
    {
        var validator = new GetServiceOrdersQueryValidator();
        var query = new GetServiceOrdersQuery { Page = 1, PageSize = 20, Status = "   " };

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetServiceOrdersQueryHandler_WithWhitespaceStatus_ShouldIgnoreStatusFilter()
    {
        var repository = new CapturingServiceOrderRepository();
        var handler = new GetServiceOrdersQueryHandler(repository);
        var query = new GetServiceOrdersQuery { Page = 1, PageSize = 10, Status = "   " };

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0, repository.LastFilterCount);
    }

    private sealed class CapturingServiceOrderRepository : IServiceOrderRepository
    {
        public int LastFilterCount { get; private set; }

        public Task<ServiceOrder?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ServiceOrder?>(null);

        public Task<(IEnumerable<ServiceOrder> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            string? orderBy,
            bool orderDescending,
            IEnumerable<Expression<Func<ServiceOrder, bool>>>? filters = null,
            CancellationToken cancellationToken = default)
        {
            LastFilterCount = filters?.Count() ?? 0;
            return Task.FromResult(((IEnumerable<ServiceOrder>)Array.Empty<ServiceOrder>(), 0));
        }

        public Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ServiceOrder?>(null);

        public Task<IEnumerable<ServiceOrder>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<ServiceOrder>>(Array.Empty<ServiceOrder>());

        public Task AddAsync(ServiceOrder entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Update(ServiceOrder entity)
        {
        }

        public void Delete(ServiceOrder entity)
        {
        }
    }
}
