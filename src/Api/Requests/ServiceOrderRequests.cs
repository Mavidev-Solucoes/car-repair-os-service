using Domain.Enums;

namespace Api.Requests;

public record AddServiceOrderItemRequest(string Description, decimal UnitPrice, int Quantity);

public record UpdateServiceOrderItemRequest(string Description, decimal UnitPrice, int Quantity);

public record UpdateServiceOrderStatusRequest(ServiceOrderStatus Status);
