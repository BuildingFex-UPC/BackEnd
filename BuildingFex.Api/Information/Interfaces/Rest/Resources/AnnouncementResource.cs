namespace BuildingFex.Api.Information.Interfaces.Rest.Resources;

public record AnnouncementResource(
    string Id,
    string Title,
    string Body,
    string Priority,
    int Duration,
    string AuthorId,
    string AuthorName,
    string CreatedAt,
    string ExpiresAt,
    string OwnerAdminId);

public record CreateAnnouncementCompatResource(
    string? Id,
    string? Title,
    string? Body,
    string? Priority,
    int Duration,
    string? AuthorId,
    string? AuthorName,
    string? CreatedAt,
    string? ExpiresAt,
    string? OwnerAdminId);

public record UpdateAnnouncementCompatResource(
    string? Title,
    string? Body,
    string? Priority,
    int Duration);
