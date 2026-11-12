
using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Shared.Domain.Model.Entities;

namespace BuildingFex.Api.SocialSpaces.Domain.Model.Aggregates;

public class SocialSpace : IAuditableEntity
{
    public int Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public int OwnerAdminId { get; private set; }
    public User? OwnerAdmin { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int? Capacity { get; private set; }
    public string ImageUrl { get; private set; } = string.Empty;
    public decimal Cost { get; private set; }

    /// <summary>Reglas de uso del área común (US19).</summary>
    public string Rules { get; private set; } = string.Empty;

    /// <summary>Indica si el área está en mantenimiento y no admite reservas (US19).</summary>
    public bool UnderMaintenance { get; private set; }

    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private SocialSpace() { }

    public static SocialSpace Create(
        string externalId,
        int ownerAdminId,
        string name,
        string description,
        int? capacity,
        string imageUrl,
        decimal cost,
        string rules = "",
        bool underMaintenance = false)
    {
        return new SocialSpace
        {
            ExternalId = externalId,
            OwnerAdminId = ownerAdminId,
            Name = name,
            Description = description,
            Capacity = capacity,
            ImageUrl = imageUrl,
            Cost = cost,
            Rules = rules ?? string.Empty,
            UnderMaintenance = underMaintenance
        };
    }

    public void Patch(
        string name,
        string description,
        int? capacity,
        string? imageUrl,
        decimal cost,
        string? rules = null,
        bool? underMaintenance = null)
    {
        Name = name;
        Description = description;
        Capacity = capacity;
        Cost = cost;

        if (imageUrl is not null)
            ImageUrl = imageUrl;

        if (rules is not null)
            Rules = rules;

        if (underMaintenance.HasValue)
            UnderMaintenance = underMaintenance.Value;
    }
}
