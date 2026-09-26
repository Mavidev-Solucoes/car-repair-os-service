using Domain.Common;
using Domain.Events;

namespace Domain.Entities;

public class Vehicle : BaseEntity
{
    private readonly List<ServiceOrder> _serviceOrders = [];

    private Vehicle()
    {
    }

    public Vehicle(Guid customerId, string brand, string model, int year, string licensePlate, string? color = null, Guid? createdUserId = null)
    {
        CustomerId = customerId;
        Brand = brand.Trim();
        Model = model.Trim();
        Year = year;
        LicensePlate = NormalizeLicensePlate(licensePlate);
        Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim();
        SetCreatedBy(createdUserId);
        RaiseDomainEvent(new VehicleCreatedDomainEvent(Id, CustomerId, LicensePlate));
    }

    public Guid CustomerId { get; private set; }
    public string Brand { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public string LicensePlate { get; private set; } = string.Empty;
    public string? Color { get; private set; }
    public Customer Customer { get; private set; } = null!;
    public IReadOnlyCollection<ServiceOrder> ServiceOrders => _serviceOrders.AsReadOnly();

    public void Update(string brand, string model, int year, string licensePlate, string? color, Guid? updatedUserId = null)
    {
        Brand = brand.Trim();
        Model = model.Trim();
        Year = year;
        LicensePlate = NormalizeLicensePlate(licensePlate);
        Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim();
        Touch(updatedUserId);
    }

    private static string NormalizeLicensePlate(string licensePlate) => new string(licensePlate.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
}
