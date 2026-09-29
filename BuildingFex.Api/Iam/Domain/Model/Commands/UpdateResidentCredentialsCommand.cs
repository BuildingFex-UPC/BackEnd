namespace BuildingFex.Api.Iam.Domain.Model.Commands;

public record UpdateResidentCredentialsCommand(
    string ExternalId,
    string Email,
    string Password);
