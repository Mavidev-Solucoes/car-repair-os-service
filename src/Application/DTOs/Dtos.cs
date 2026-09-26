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
