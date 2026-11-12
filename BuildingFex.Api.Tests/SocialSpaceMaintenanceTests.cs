using System.Text.Json;
using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Iam.Domain.Repositories;
using BuildingFex.Api.SocialSpaces.Application.Internal;
using BuildingFex.Api.SocialSpaces.Domain.Model.Aggregates;
using BuildingFex.Api.SocialSpaces.Domain.Repositories;
using BuildingFex.Api.SocialSpaces.Interfaces.Rest.Compat;
using BuildingFex.Api.SocialSpaces.Interfaces.Rest.Transform;
using BuildingFex.Api.Shared.Domain.Repositories;
using BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BuildingFex.Api.Tests;

/// <summary>
/// US19 — Reglas y mantenimiento de áreas comunes:
/// un espacio en mantenimiento rechaza nuevas reservas (AC02)
/// y los campos rules/underMaintenance se exponen en la API.
/// </summary>
public class SocialSpaceMaintenanceTests
{
    private readonly Mock<IReservationRepository> _reservationRepoMock = new();
    private readonly Mock<ISocialSpaceRepository> _spaceRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=buildingfex_test;Username=test;Password=test")
            .Options;
        return new AppDbContext(options);
    }

    private ReservationsCompatController CreateController()
    {
        var resolver = new SocialSpacesOwnerResolver(_userRepoMock.Object);
        return new ReservationsCompatController(
            _reservationRepoMock.Object,
            _spaceRepoMock.Object,
            resolver,
            _unitOfWorkMock.Object,
            CreateDbContext());
    }

    private void SetupAdminOwner()
    {
        var admin = User.CreateAdmin("admin-1", "Admin", "admin@test.com", "hash");
        _userRepoMock.Setup(u => u.FindByExternalIdAsync("admin-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);
    }

    private static JsonElement Payload() => JsonSerializer.Deserialize<JsonElement>("""
    {
        "ownerAdminId": "admin-1",
        "spaceId": "space-under-maintenance",
        "residentId": "resident-1",
        "date": "2027-01-10",
        "startTime": "10:00",
        "endTime": "11:00"
    }
    """);

    [Fact]
    public async Task Create_UnderMaintenanceSpace_Returns400SpaceUnderMaintenance()
    {
        // Arrange
        SetupAdminOwner();
        var space = SocialSpace.Create(
            "space-under-maintenance", 1, "Salón", "", null, "", 0m,
            underMaintenance: true);
        _spaceRepoMock
            .Setup(s => s.FindByExternalIdAsync("space-under-maintenance", It.IsAny<CancellationToken>()))
            .ReturnsAsync(space);

        var controller = CreateController();

        // Act
        var result = await controller.Create(Payload(), CancellationToken.None);

        // Assert — rechazado antes de tocar la transacción/candado.
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        Assert.Equal(
            "SPACE_UNDER_MAINTENANCE",
            BodyCode(badRequest.Value));
        _reservationRepoMock.Verify(
            r => r.AddAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Create_UnknownSpace_Returns400SpaceNotFound()
    {
        // Arrange
        SetupAdminOwner();
        _spaceRepoMock
            .Setup(s => s.FindByExternalIdAsync("space-under-maintenance", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocialSpace?)null);

        var controller = CreateController();

        // Act
        var result = await controller.Create(Payload(), CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        Assert.Equal("SPACE_NOT_FOUND", BodyCode(badRequest.Value));
    }

    /// <summary>Reads `code` from the anonymous error body returned by the controller.</summary>
    private static string? BodyCode(object? value) =>
        value is null
            ? null
            : JsonSerializer.SerializeToElement(value).GetProperty("code").GetString();

    [Fact]
    public void SpaceToJson_ExposesRulesAndUnderMaintenance()
    {
        // Arrange
        var space = SocialSpace.Create(
            "space-1", 1, "Piscina", "Descripción", 30, "", 10m,
            rules: "No correr dentro del área.",
            underMaintenance: true);

        // Act
        var json = JsonSerializer.SerializeToElement(SocialSpacesCompatSerializer.SpaceToJson(space));

        // Assert
        Assert.Equal("No correr dentro del área.", json.GetProperty("rules").GetString());
        Assert.True(json.GetProperty("underMaintenance").GetBoolean());
    }

    [Fact]
    public void SocialSpace_Create_DefaultsToNoRulesAndAvailable()
    {
        // Act — espacios existentes se crean sin los campos nuevos (retrocompatible).
        var space = SocialSpace.Create("space-2", 1, "Terraza", "", null, "", 0m);

        // Assert
        Assert.Equal(string.Empty, space.Rules);
        Assert.False(space.UnderMaintenance);
    }

    [Fact]
    public void SocialSpace_Patch_KeepsFieldsWhenNotSent()
    {
        // Arrange
        var space = SocialSpace.Create(
            "space-3", 1, "Salón", "", null, "", 0m,
            rules: "Regla original",
            underMaintenance: true);

        // Act — patch sin tocar rules/underMaintenance.
        space.Patch("Salón 2", "Nueva descripción", null, null, 5m);

        // Assert
        Assert.Equal("Regla original", space.Rules);
        Assert.True(space.UnderMaintenance);
        Assert.Equal("Salón 2", space.Name);
    }

    [Fact]
    public void SocialSpace_Patch_UpdatesRulesAndMaintenanceWhenSent()
    {
        // Arrange
        var space = SocialSpace.Create("space-4", 1, "Salón", "", null, "", 0m);

        // Act
        space.Patch("Salón", "", null, null, 0m, rules: "Nueva regla", underMaintenance: true);

        // Assert
        Assert.Equal("Nueva regla", space.Rules);
        Assert.True(space.UnderMaintenance);
    }
}
