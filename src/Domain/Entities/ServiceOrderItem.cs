using Domain.Common;

namespace Domain.Entities;

public class ServiceOrderItem : BaseEntity
{
    private ServiceOrderItem()
    {
    }

    internal ServiceOrderItem(Guid serviceOrderId, string description, decimal unitPrice, int quantity, Guid? createdUserId = null)
    {
        ServiceOrderId = serviceOrderId;
        Description = description.Trim();
        UnitPrice = unitPrice;
        Quantity = quantity;
        SetCreatedBy(createdUserId);
    }

    public Guid ServiceOrderId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal Total => UnitPrice * Quantity;
    public ServiceOrder ServiceOrder { get; private set; } = null!;

    internal void Update(string description, decimal unitPrice, int quantity, Guid? updatedUserId = null)
    {
        Description = description.Trim();
        UnitPrice = unitPrice;
        Quantity = quantity;
        Touch(updatedUserId);
    }
}
