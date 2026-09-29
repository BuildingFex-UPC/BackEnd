namespace BuildingFex.Api.Iam.Domain.Model.Commands;

public record CreateResidentCommand(
    string ExternalId,
    string Name,
    string Floor,
    string Code,
    string OwnerAdminExternalId,
    string? AdmissionDate);
