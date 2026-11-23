using BuildingFex.Api.Iam.Domain.Repositories;
using BuildingFex.Api.Incidents.Application.CommandServices;
using BuildingFex.Api.Incidents.Domain.Model;
using BuildingFex.Api.Incidents.Domain.Model.Aggregates;
using BuildingFex.Api.Incidents.Domain.Model.Commands;
using BuildingFex.Api.Incidents.Domain.Repositories;
using BuildingFex.Api.Notifications.Application.CommandServices;
using BuildingFex.Api.Shared.Application.Model;
using BuildingFex.Api.Shared.Domain.Repositories;

namespace BuildingFex.Api.Incidents.Application.Internal.CommandServices;

public class IncidentCommandService(
    IIncidentRepository incidentRepository,
    IUserRepository userRepository, 
    INotificationCommandService notificationCommandService,
    IUnitOfWork unitOfWork) : IIncidentCommandService
{
    public async Task<Result<Incident>> Handle(
        CreateIncidentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Description))
            return Result<Incident>.Failure(IncidentError.DescriptionRequired, "La descripción es obligatoria.");

        if (string.IsNullOrWhiteSpace(command.OwnerAdminExternalId))
            return Result<Incident>.Failure(IncidentError.OwnerAdminRequired, "ownerAdminId es obligatorio.");

        var owner = await userRepository.FindByExternalIdAsync(command.OwnerAdminExternalId, cancellationToken);
        if (owner is null || owner.Role != "admin")
            return Result<Incident>.Failure(IncidentError.OwnerAdminRequired, "Administrador no encontrado.");

        DateTimeOffset? reportedAt = null;
        if (!string.IsNullOrWhiteSpace(command.CreatedAt) &&
            DateTimeOffset.TryParse(command.CreatedAt, out var parsed))
            reportedAt = parsed;

        var externalId = string.IsNullOrWhiteSpace(command.ExternalId)
            ? $"incident-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
            : command.ExternalId.Trim();

        var incident = Incident.Create(
            externalId,
            owner.Id,
            command.Description,
            command.Status,
            command.ResidentExternalId,
            command.ResidentName,
            command.Provider,
            reportedAt);

        await incidentRepository.AddAsync(incident, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        // Notificar al administrador sobre el nuevo reporte de incidente
        try
        {
            var residentDisplayName = !string.IsNullOrWhiteSpace(command.ResidentName)
                ? command.ResidentName
                : (!string.IsNullOrWhiteSpace(command.ResidentExternalId) ? command.ResidentExternalId : "un residente");

            await notificationCommandService.CreateNotificationAsync(
                recipientExternalId: owner.ExternalId,
                recipientRole: "admin",
                title: "Nuevo incidente reportado",
                message: $"El residente {residentDisplayName} ha reportado: {command.Description}",
                type: "incident_reported",
                relatedEntityId: externalId,
                ct: cancellationToken);
        }
        catch
        {
            // La notificación es complementaria; no interrumpe la creación del incidente
        }

        var created = await incidentRepository.FindByExternalIdAsync(externalId, cancellationToken);
        return Result<Incident>.Success(created!);
    }

    public async Task<Result<Incident>> Handle(
        UpdateIncidentCommand command,
        CancellationToken cancellationToken = default)
    {
        var incident = await incidentRepository.FindByExternalIdAsync(command.ExternalId, cancellationToken);
        if (incident is null)
            return Result<Incident>.Failure(IncidentError.IncidentNotFound, "Incidencia no encontrada.");

        if (string.IsNullOrWhiteSpace(command.Description))
            return Result<Incident>.Failure(IncidentError.DescriptionRequired, "La descripción es obligatoria.");

        var previousStatus = incident.Status;

        try
        {
            incident.Update(
                command.Description,
                command.Status,
                command.Provider,
                command.ResidentExternalId,
                command.ResidentName);
        }
        catch (ArgumentException ex)
        {
            return Result<Incident>.Failure(IncidentError.InvalidStatus, ex.Message);
        }

        incidentRepository.Update(incident);
        await unitOfWork.CompleteAsync(cancellationToken);

        // Si el estado cambió a 'resolved', notificar al residente que reportó
        var newStatus = incident.Status;
        if (string.Equals(newStatus, IncidentStatuses.Resolved, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(previousStatus, IncidentStatuses.Resolved, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(incident.ResidentExternalId))
        {
            try
            {
                await notificationCommandService.CreateNotificationAsync(
                    recipientExternalId: incident.ResidentExternalId,
                    recipientRole: "resident",
                    title: "Incidente resuelto",
                    message: $"Tu incidente '{incident.Description}' ha sido resuelto.",
                    type: "incident_resolved",
                    relatedEntityId: incident.ExternalId,
                    ct: cancellationToken);
            }
            catch
            {
                // La notificación es complementaria; no interrumpe la actualización del incidente
            }
        }

        var updated = await incidentRepository.FindByExternalIdAsync(command.ExternalId, cancellationToken);
        return Result<Incident>.Success(updated!);
    }

    public async Task<Result<Incident>> Handle(
        DeleteIncidentCommand command,
        CancellationToken cancellationToken = default)
    {
        var incident = await incidentRepository.FindByExternalIdAsync(command.ExternalId, cancellationToken);
        if (incident is null)
            return Result<Incident>.Failure(IncidentError.IncidentNotFound, "Incidencia no encontrada.");

        incidentRepository.Remove(incident);
        await unitOfWork.CompleteAsync(cancellationToken);

        return Result<Incident>.Success(incident);
    }
}



