using Domain.Common;
using Domain.Events;
using Domain.ValueObjects;

namespace Domain.Entities;

public class Customer : BaseEntity
{
    private readonly List<Vehicle> _vehicles = [];

    private Customer()
    {
    }

    public Customer(string name, string personalId, string email, string telephone, Guid? createdUserId = null)
    {
        Name = NormalizeName(name);
        PersonalId = new PersonalId(personalId).Value;
        Email = NormalizeEmail(email);
        Telephone = new PhoneNumber(telephone).Value;
        IsActive = true;
        SetCreatedBy(createdUserId);
        RaiseDomainEvent(new CustomerCreatedDomainEvent(Id, PersonalId));
    }

    public string Name { get; private set; } = string.Empty;
    public string PersonalId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Telephone { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public IReadOnlyCollection<Vehicle> Vehicles => _vehicles.AsReadOnly();

    public void Update(string name, string email, string telephone, bool isActive, Guid? updatedUserId = null)
    {
        Name = NormalizeName(name);
        Email = NormalizeEmail(email);
        Telephone = new PhoneNumber(telephone).Value;
        IsActive = isActive;
        Touch(updatedUserId);
    }

    private static string NormalizeName(string name) => string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
