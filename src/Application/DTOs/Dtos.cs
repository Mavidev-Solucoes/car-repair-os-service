using Domain.Enums;

namespace Application.DTOs;

public record CustomerDto(
    Guid Id,
    string Name,
    string PersonalId,
    string Email,
    string Telephone,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IEnumerable<VehicleDto>? Vehicles = null);

public record VehicleDto(
    Guid Id,
    Guid CustomerId,
    string Brand,
    string Model,
    int Year,
    string LicensePlate,
    string? Color,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record ServiceOrderItemDto(
    Guid Id,
    Guid ServiceOrderId,
    string Description,
    decimal UnitPrice,
    int Quantity,
    decimal Total);

public record ServiceStatusHistoryDto(
    Guid Id,
    Guid ServiceOrderId,
    ServiceOrderStatus? FromStatus,
    ServiceOrderStatus ToStatus,
    DateTime ChangedAt,
    Guid? ChangedByUserId);

public record ServiceOrderDto(
    Guid Id,
    Guid VehicleId,
    Guid CustomerId,
    ServiceOrderStatus Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IEnumerable<ServiceOrderItemDto> Items,
    IEnumerable<ServiceStatusHistoryDto> StatusHistory);
