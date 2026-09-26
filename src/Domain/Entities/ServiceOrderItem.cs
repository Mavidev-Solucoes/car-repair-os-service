using Domain.Common;

namespace Domain.Entities;

public class ServiceOrderItem : BaseEntity
{
    private ServiceOrderItem()
    {
    }

    public ServiceOrderItem(Guid serviceOrderId, Guid serviceItemId, string description, decimal price, int quantity, Guid? createdUserId = null)
    {
        ServiceOrderId = serviceOrderId;
        ServiceItemId = serviceItemId;
        Description = description.Trim();
        Price = price;
        Quantity = quantity;
        SetCreatedBy(createdUserId);
    }

    public Guid ServiceOrderId { get; private set; }
    public Guid ServiceItemId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int Quantity { get; private set; }
}
