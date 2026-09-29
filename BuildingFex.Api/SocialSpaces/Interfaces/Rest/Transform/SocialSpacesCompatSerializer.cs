using System.Text.Json;
using BuildingFex.Api.SocialSpaces.Domain.Model.Aggregates;

namespace BuildingFex.Api.SocialSpaces.Interfaces.Rest.Transform;

public static class SocialSpacesCompatSerializer
{
    public static object SpaceToJson(SocialSpace space) => new
    {
        id = space.ExternalId,
        name = space.Name,
        description = space.Description,
        capacity = space.Capacity,
        imageUrl = space.ImageUrl,
        ownerAdminId = space.OwnerAdmin?.ExternalId ?? string.Empty,
    };

    public static object ReservationToJson(Reservation reservation)
    {
        object[] guests;
        try
        {
            guests = JsonSerializer.Deserialize<object[]>(
                string.IsNullOrWhiteSpace(reservation.GuestsJson) ? "[]" : reservation.GuestsJson) ?? [];
        }
        catch
        {
            guests = [];
        }

        return new
        {
            id = reservation.ExternalId,
            spaceId = reservation.SpaceExternalId,
            residentId = reservation.ResidentExternalId,
            residentName = reservation.ResidentName,
            residentCode = reservation.ResidentCode,
            date = reservation.Date,
            startTime = reservation.StartTime,
            endTime = reservation.EndTime,
            guests,
            guestInviteToken = reservation.GuestInviteToken,
            ownerAdminId = reservation.OwnerAdmin?.ExternalId ?? string.Empty,
        };
    }
}
