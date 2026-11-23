using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Iam.Domain.Repositories;
using BuildingFex.Api.Incidents.Application.Internal.CommandServices;
using BuildingFex.Api.Incidents.Domain.Model;
using BuildingFex.Api.Incidents.Domain.Model.Aggregates;
using BuildingFex.Api.Incidents.Domain.Model.Commands;
using BuildingFex.Api.Incidents.Domain.Repositories;
using BuildingFex.Api.Notifications.Application.CommandServices;
using BuildingFex.Api.Notifications.Application.Internal.CommandServices;
using BuildingFex.Api.Notifications.Domain.Model.Aggregates;
using BuildingFex.Api.Notifications.Domain.Repositories;
using BuildingFex.Api.Shared.Domain.Repositories;
using Moq;
using Xunit;

namespace BuildingFex.Api.Tests;

public class IncidentNotificationTests
{
    private readonly Mock<IIncidentRepository> _incidentRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<INotificationRepository> _notificationRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private IncidentCommandService CreateIncidentCommandService(INotificationCommandService notifService)
    {
        return new IncidentCommandService(
            _incidentRepoMock.Object,
            _userRepoMock.Object,
            notifService,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateIncident_NotifiesAdministrator()
    {
        // Arrange
        var admin = User.CreateAdmin("admin-1", "Admin Name", "admin@test.com", "hash");
        _userRepoMock.Setup(u => u.FindByExternalIdAsync("admin-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);

        Notification? createdNotification = null;
        _notificationRepoMock.Setup(n => n.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, _) => createdNotification = n)
            .Returns(Task.CompletedTask);

        var notifService = new NotificationCommandService(_notificationRepoMock.Object, _unitOfWorkMock.Object);
        var incidentService = CreateIncidentCommandService(notifService);

        var command = new CreateIncidentCommand(
            ExternalId: "inc-1",
            OwnerAdminExternalId: "admin-1",
            Description: "Tubería rota en piso 3",
            Status: "open",
            ResidentExternalId: "res-101",
            ResidentName: "Juan Pérez",
            Provider: null,
            CreatedAt: null);

        _incidentRepoMock.Setup(r => r.FindByExternalIdAsync("inc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Incident.Create("inc-1", admin.Id, "Tubería rota en piso 3", "open", "res-101", "Juan Pérez"));

        // Act
        var result = await incidentService.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(createdNotification);
        Assert.Equal("admin-1", createdNotification.RecipientExternalId);
        Assert.Equal("admin", createdNotification.RecipientRole);
        Assert.Equal("incident_reported", createdNotification.Type);
        Assert.Equal("Nuevo incidente reportado", createdNotification.Title);
        Assert.Contains("Juan Pérez", createdNotification.Message);
        Assert.Contains("Tubería rota en piso 3", createdNotification.Message);
    }

    [Fact]
    public async Task UpdateIncident_ToResolved_NotifiesResident()
    {
        // Arrange
        var existingIncident = Incident.Create(
            "inc-2",
            1,
            "Luz del pasillo quemada",
            "open",
            "res-202",
            "María López");

        _incidentRepoMock.Setup(r => r.FindByExternalIdAsync("inc-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingIncident);

        Notification? createdNotification = null;
        _notificationRepoMock.Setup(n => n.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, _) => createdNotification = n)
            .Returns(Task.CompletedTask);

        var notifService = new NotificationCommandService(_notificationRepoMock.Object, _unitOfWorkMock.Object);
        var incidentService = CreateIncidentCommandService(notifService);

        var command = new UpdateIncidentCommand(
            ExternalId: "inc-2",
            Description: "Luz del pasillo cambiada",
            Status: "resolved",
            Provider: "Electricista SAC",
            ResidentExternalId: "res-202",
            ResidentName: "María López");

        // Act
        var result = await incidentService.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(createdNotification);
        Assert.Equal("res-202", createdNotification.RecipientExternalId);
        Assert.Equal("resident", createdNotification.RecipientRole);
        Assert.Equal("incident_resolved", createdNotification.Type);
        Assert.Equal("Incidente resuelto", createdNotification.Title);
        Assert.Contains("resuelto", createdNotification.Message);
    }

    [Fact]
    public async Task UpdateIncident_ToInProgress_DoesNotNotifyResolved()
    {
        // Arrange
        var existingIncident = Incident.Create(
            "inc-3",
            1,
            "Ascensor averiado",
            "open",
            "res-303",
            "Carlos Diaz");

        _incidentRepoMock.Setup(r => r.FindByExternalIdAsync("inc-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingIncident);

        var notifServiceMock = new Mock<INotificationCommandService>();
        var incidentService = CreateIncidentCommandService(notifServiceMock.Object);

        var command = new UpdateIncidentCommand(
            ExternalId: "inc-3",
            Description: "Ascensor en reparación",
            Status: "in-progress",
            Provider: "Schindler",
            ResidentExternalId: "res-303",
            ResidentName: "Carlos Diaz");

        // Act
        var result = await incidentService.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        notifServiceMock.Verify(n => n.CreateNotificationAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            "incident_resolved",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Notification_MarkAsRead_UpdatesIsReadAndTimestamp()
    {
        // Arrange
        var notif = Notification.Create(
            "notif-1",
            "admin-1",
            "admin",
            "Test",
            "Test Message");

        _notificationRepoMock.Setup(r => r.FindByExternalIdAsync("notif-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(notif);

        var service = new NotificationCommandService(_notificationRepoMock.Object, _unitOfWorkMock.Object);

        // Act
        var result = await service.MarkAsReadAsync("notif-1");

        // Assert
        Assert.True(result);
        Assert.True(notif.IsRead);
        Assert.NotNull(notif.ReadAt);
        _notificationRepoMock.Verify(r => r.Update(notif), Times.Once);
        _unitOfWorkMock.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

