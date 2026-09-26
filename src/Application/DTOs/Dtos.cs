namespace Application.DTOs;

public record CustomerDto(
    Guid Id,
    string Name,
    string PersonalId,
    string Email,
    string Telephone,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record VehicleDto(
    Guid Id,
    Guid CustomerId,
    string Brand,
    string Model,
    int Year,
    string LicensePlate,
    string? Color,
    DateTime CreatedAt);

public record ServiceOrderItemDto(
    Guid Id,
    Guid ServiceOrderId,
    Guid ServiceItemId,
    string Description,
    decimal Price,
    int Quantity);

public record ServiceStatusHistoryDto(
    Guid Id,
    Guid ServiceOrderId,
    string? FromStatus,
    string ToStatus,
    DateTime ChangedAt,
    Guid? ChangedByUserId);

public record ServiceOrderDto(
    Guid Id,
    Guid VehicleId,
    Guid CustomerId,
    Guid AssignedUserId,
    string Status,
    decimal TotalPrice,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IEnumerable<ServiceOrderItemDto> ServiceItems,
    IEnumerable<ServiceStatusHistoryDto> StatusHistory);

