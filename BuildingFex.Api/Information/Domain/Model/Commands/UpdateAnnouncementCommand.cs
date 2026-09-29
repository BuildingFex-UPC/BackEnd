namespace BuildingFex.Api.Information.Domain.Model.Commands;

public record UpdateAnnouncementCommand(
    string ExternalId,
    string Title,
    string Body,
    string Priority,
    int Duration);
