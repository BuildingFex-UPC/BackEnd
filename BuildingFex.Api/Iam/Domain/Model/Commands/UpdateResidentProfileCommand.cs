namespace BuildingFex.Api.Iam.Domain.Model.Commands;

/// <summary>
/// Actualiza los datos editables del perfil (residente o admin). El correo sí
/// se incluye (el usuario puede cambiar su login) pero se valida unicidad en
/// el servicio. Los residentes también pueden cambiar su departamento: en ese
/// caso Floor y Code viajan juntos (el código conserva el sufijo de invitación).
/// </summary>
public record UpdateResidentProfileCommand(
    string ExternalId,
    string Name,
    string? Dni,
    string? Address,
    string? Floor,
    string? Email = null,
    string? Avatar = null,
    string? Company = null,
    string? Ruc = null,
    string? Code = null);
