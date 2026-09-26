using Domain.Common;

namespace Domain.Entities;

public class Vehicle : BaseEntity
{
    private Vehicle()
    {
    }

    public Vehicle(Guid customerId, string brand, string model, int year, string licensePlate, string? color = null, Guid? createdUserId = null)
    {
        CustomerId = customerId;
        Brand = brand;
        Model = model;
        Year = year;
        LicensePlate = NormalizeLicensePlate(licensePlate);
        Color = color;
        SetCreatedBy(createdUserId);
    }

    public Guid CustomerId { get; private set; }
    public string Brand { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public string LicensePlate { get; private set; } = string.Empty;
    public string? Color { get; private set; }

    public void Update(string brand, string model, int year, string licensePlate, string? color, Guid? updatedUserId = null)
    {
        Brand = brand;
        Model = model;
        Year = year;
        LicensePlate = NormalizeLicensePlate(licensePlate);
        Color = color;
        Touch(updatedUserId);
    }

    private static string NormalizeLicensePlate(string value) =>
        new string(value.Where(c => c != '-').ToArray()).ToUpperInvariant();
}
