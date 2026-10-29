namespace BuildingFex.Api.Iam.Domain.Model.Commands;

/// <summary>
/// Actualiza los datos editables del perfil de un residente. El correo sí se
/// incluye (el residente puede cambiar su login) pero se valida unicidad en el
/// servicio; el código de departamento queda fuera porque lo asigna el admin.
/// </summary>
public record UpdateResidentProfileCommand(
    string ExternalId,
    string Name,
    string? Dni,
    string? Address,
    string? Floor,
    string? Email = null,
    string? Avatar = null);
