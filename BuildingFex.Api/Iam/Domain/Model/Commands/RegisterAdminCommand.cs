namespace BuildingFex.Api.Iam.Domain.Model.Commands;

public record RegisterAdminCommand(
    string Name,
    string Email,
    string Password,
    string? Dni,
    string? Address,
    string? Company,
    string? Ruc);
