namespace BuildingFex.Api.Iam.Domain.Model.Commands;

/// <summary>
/// Actualiza los datos editables del perfil de un residente. No incluye el correo
/// (identificador de login) ni el código de departamento (lo asigna el admin).
/// </summary>
public record UpdateResidentProfileCommand(
    string ExternalId,
    string Name,
    string? Dni,
    string? Address,
    string? Floor);
