using BuildingFex.Api.SocialSpaces.Application.Internal;
using BuildingFex.Api.SocialSpaces.Domain.Model.Aggregates;
using BuildingFex.Api.SocialSpaces.Domain.Repositories;
using BuildingFex.Api.SocialSpaces.Interfaces.Rest.Transform;
using BuildingFex.Api.Shared.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BuildingFex.Api.SocialSpaces.Interfaces.Rest.Compat;

[ApiController]
[Route("reservations")]
[Authorize]
public class ReservationsCompatController(
    IReservationRepository reservationRepository,
    SocialSpacesOwnerResolver ownerResolver,
    IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? ownerAdminId,
        [FromQuery] string? spaceId,
        [FromQuery] string? residentId,
        [FromQuery] string? date,
        [FromQuery] string? guestInviteToken,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(guestInviteToken) && !(User.Identity?.IsAuthenticated ?? false))
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Autenticación requerida." });

        var reservations = await reservationRepository.ListAsync(
            ownerAdminId,
            spaceId,
            residentId,
            date,
            guestInviteToken,
            ct);

        return Ok(reservations.Select(SocialSpacesCompatSerializer.ReservationToJson));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var reservation = await reservationRepository.FindByExternalIdAsync(id, ct);
        if (reservation is null)
            return NotFound(new { code = "RESERVATION_NOT_FOUND", message = "Reserva no encontrada." });

        return Ok(SocialSpacesCompatSerializer.ReservationToJson(reservation));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] JsonElement body, CancellationToken ct)
    {
        var ownerExternalId = body.TryGetProperty("ownerAdminId", out var ownerProp)
            ? ownerProp.GetString()
            : null;

        var owner = await ownerResolver.ResolveOwnerAdminAsync(ownerExternalId, ct);
        if (owner is null)
            return BadRequest(new { code = "OWNER_ADMIN_REQUIRED", message = "ownerAdminId es obligatorio." });

        var spaceId = body.TryGetProperty("spaceId", out var spaceProp) ? spaceProp.GetString()?.Trim() : null;
        var residentId = body.TryGetProperty("residentId", out var resProp) ? resProp.GetString()?.Trim() : null;
        var reservationDate = body.TryGetProperty("date", out var dateProp) ? dateProp.GetString()?.Trim() : null;
        var startTime = body.TryGetProperty("startTime", out var startProp) ? startProp.GetString()?.Trim() : null;
        var endTime = body.TryGetProperty("endTime", out var endProp) ? endProp.GetString()?.Trim() : null;

        if (string.IsNullOrWhiteSpace(spaceId) ||
            string.IsNullOrWhiteSpace(residentId) ||
            string.IsNullOrWhiteSpace(reservationDate) ||
            string.IsNullOrWhiteSpace(startTime) ||
            string.IsNullOrWhiteSpace(endTime))
        {
            return BadRequest(new { code = "RESERVATION_FIELDS_REQUIRED", message = "Campos obligatorios incompletos." });
        }

        if (!IsValidTimeRange(startTime, endTime))
            return BadRequest(new { code = "RESERVATION_TIME_INVALID", message = "Horario inválido." });

        var existing = await reservationRepository.ListAsync(
            owner.ExternalId,
            spaceId,
            null,
            reservationDate,
            null,
            ct);

        if (existing.Any(r => ReservationOverlapHelper.Overlaps(r, reservationDate, startTime, endTime)))
            return BadRequest(new { code = "RESERVATION_OVERLAP", message = "La reserva se solapa con otra existente." });

        var externalId = body.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String
            ? idProp.GetString() ?? $"reservation-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
            : $"reservation-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        var guestsJson = body.TryGetProperty("guests", out var guestsProp)
            ? guestsProp.GetRawText()
            : "[]";

        string? guestInviteToken = body.TryGetProperty("guestInviteToken", out var tokenProp) &&
                                   tokenProp.ValueKind == JsonValueKind.String
            ? tokenProp.GetString()
            : null;

        var reservation = Reservation.Create(
            externalId,
            owner.Id,
            spaceId,
            residentId,
            body.TryGetProperty("residentName", out var nameProp) ? nameProp.GetString() ?? string.Empty : string.Empty,
            body.TryGetProperty("residentCode", out var codeProp) ? codeProp.GetString() ?? string.Empty : string.Empty,
            reservationDate,
            startTime,
            endTime,
            guestsJson,
            guestInviteToken);

        await reservationRepository.AddAsync(reservation, ct);
        await unitOfWork.CompleteAsync(ct);

        var created = await reservationRepository.FindByExternalIdAsync(externalId, ct);
        return StatusCode(StatusCodes.Status201Created, SocialSpacesCompatSerializer.ReservationToJson(created!));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(string id, [FromBody] JsonElement body, CancellationToken ct)
    {
        var reservation = await reservationRepository.FindByExternalIdAsync(id, ct);
        if (reservation is null)
            return NotFound(new { code = "RESERVATION_NOT_FOUND", message = "Reserva no encontrada." });

        var guestsJson = body.TryGetProperty("guests", out var guestsProp)
            ? guestsProp.GetRawText()
            : reservation.GuestsJson;

        string? guestInviteToken = reservation.GuestInviteToken;
        if (body.TryGetProperty("guestInviteToken", out var tokenProp))
        {
            guestInviteToken = tokenProp.ValueKind == JsonValueKind.Null
                ? null
                : tokenProp.GetString();
        }

        reservation.PatchGuests(guestsJson, guestInviteToken);
        reservationRepository.Update(reservation);
        await unitOfWork.CompleteAsync(ct);

        var updated = await reservationRepository.FindByExternalIdAsync(id, ct);
        return Ok(SocialSpacesCompatSerializer.ReservationToJson(updated!));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var reservation = await reservationRepository.FindByExternalIdAsync(id, ct);
        if (reservation is null)
            return NotFound(new { code = "RESERVATION_NOT_FOUND", message = "Reserva no encontrada." });

        reservationRepository.Remove(reservation);
        await unitOfWork.CompleteAsync(ct);
        return NoContent();
    }

    private static bool IsValidTimeRange(string startTime, string endTime)
    {
        var start = ParseMinutes(startTime);
        var end = ParseMinutes(endTime);
        return start.HasValue && end.HasValue && start < end;
    }

    private static int? ParseMinutes(string time)
    {
        var parts = time.Split(':');
        if (parts.Length < 2)
            return null;
        if (!int.TryParse(parts[0], out var h) || !int.TryParse(parts[1], out var m))
            return null;
        return h * 60 + m;
    }
}
