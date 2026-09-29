using BuildingFex.Api.Iam.Domain.Repositories;
using BuildingFex.Api.Information.Application.CommandServices;
using BuildingFex.Api.Information.Domain.Model;
using BuildingFex.Api.Information.Domain.Model.Aggregates;
using BuildingFex.Api.Information.Domain.Model.Commands;
using BuildingFex.Api.Information.Domain.Repositories;
using BuildingFex.Api.Shared.Application.Model;
using BuildingFex.Api.Shared.Domain.Repositories;

namespace BuildingFex.Api.Information.Application.Internal.CommandServices;

public class AnnouncementCommandService(
    IAnnouncementRepository announcementRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IAnnouncementCommandService
{
    public async Task<Result<Announcement>> Handle(
        CreateAnnouncementCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
            return Result<Announcement>.Failure(AnnouncementError.TitleRequired, "El título es obligatorio.");

        if (string.IsNullOrWhiteSpace(command.OwnerAdminExternalId))
            return Result<Announcement>.Failure(AnnouncementError.OwnerAdminRequired, "ownerAdminId es obligatorio.");

        var owner = await userRepository.FindByExternalIdAsync(command.OwnerAdminExternalId, cancellationToken);
        if (owner is null || owner.Role != "admin")
            return Result<Announcement>.Failure(AnnouncementError.OwnerAdminRequired, "Administrador no encontrado.");

        DateTimeOffset? expiresAt = null;
        if (!string.IsNullOrWhiteSpace(command.ExpiresAt) &&
            DateTimeOffset.TryParse(command.ExpiresAt, out var parsedExpires))
            expiresAt = parsedExpires;

        DateTimeOffset? createdAt = null;
        if (!string.IsNullOrWhiteSpace(command.CreatedAt) &&
            DateTimeOffset.TryParse(command.CreatedAt, out var parsedCreated))
            createdAt = parsedCreated;

        var externalId = string.IsNullOrWhiteSpace(command.ExternalId)
            ? $"ann-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
            : command.ExternalId.Trim();

        var announcement = Announcement.Create(
            externalId,
            owner.Id,
            command.Title,
            command.Body,
            command.Priority,
            command.Duration,
            command.AuthorName,
            expiresAt,
            createdAt);

        await announcementRepository.AddAsync(announcement, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        var created = await announcementRepository.FindByExternalIdAsync(externalId, cancellationToken);
        return Result<Announcement>.Success(created!);
    }

    public async Task<Result<Announcement>> Handle(
        UpdateAnnouncementCommand command,
        CancellationToken cancellationToken = default)
    {
        var announcement = await announcementRepository.FindByExternalIdAsync(command.ExternalId, cancellationToken);
        if (announcement is null)
            return Result<Announcement>.Failure(AnnouncementError.AnnouncementNotFound, "Comunicado no encontrado.");

        if (string.IsNullOrWhiteSpace(command.Title))
            return Result<Announcement>.Failure(AnnouncementError.TitleRequired, "El título es obligatorio.");

        announcement.Update(
            command.Title,
            command.Body,
            command.Priority,
            command.Duration);

        announcementRepository.Update(announcement);
        await unitOfWork.CompleteAsync(cancellationToken);

        var updated = await announcementRepository.FindByExternalIdAsync(command.ExternalId, cancellationToken);
        return Result<Announcement>.Success(updated!);
    }

    public async Task<Result<Announcement>> Handle(
        DeleteAnnouncementCommand command,
        CancellationToken cancellationToken = default)
    {
        var announcement = await announcementRepository.FindByExternalIdAsync(command.ExternalId, cancellationToken);
        if (announcement is null)
            return Result<Announcement>.Failure(AnnouncementError.AnnouncementNotFound, "Comunicado no encontrado.");

        announcementRepository.Remove(announcement);
        await unitOfWork.CompleteAsync(cancellationToken);

        return Result<Announcement>.Success(announcement);
    }
}
