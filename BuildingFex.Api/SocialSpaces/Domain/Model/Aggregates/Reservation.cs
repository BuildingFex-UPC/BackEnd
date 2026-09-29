using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Shared.Domain.Model.Entities;

namespace BuildingFex.Api.SocialSpaces.Domain.Model.Aggregates;

public class Reservation : IAuditableEntity
{
    public int Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public int OwnerAdminId { get; private set; }
    public User? OwnerAdmin { get; private set; }
    public string SpaceExternalId { get; private set; } = string.Empty;
    public string ResidentExternalId { get; private set; } = string.Empty;
    public string ResidentName { get; private set; } = string.Empty;
    public string ResidentCode { get; private set; } = string.Empty;
    public string Date { get; private set; } = string.Empty;
    public string StartTime { get; private set; } = string.Empty;
    public string EndTime { get; private set; } = string.Empty;
    public string GuestsJson { get; private set; } = "[]";
    public string? GuestInviteToken { get; private set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private Reservation() { }

    public static Reservation Create(
        string externalId,
        int ownerAdminId,
        string spaceExternalId,
        string residentExternalId,
        string residentName,
        string residentCode,
        string date,
        string startTime,
        string endTime,
        string guestsJson,
        string? guestInviteToken)
    {
        return new Reservation
        {
            ExternalId = externalId,
            OwnerAdminId = ownerAdminId,
            SpaceExternalId = spaceExternalId,
            ResidentExternalId = residentExternalId,
            ResidentName = residentName,
            ResidentCode = residentCode,
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            GuestsJson = guestsJson,
            GuestInviteToken = guestInviteToken,
        };
    }

    public void PatchGuests(string guestsJson, string? guestInviteToken)
    {
        GuestsJson = guestsJson;
        GuestInviteToken = guestInviteToken;
    }
}
