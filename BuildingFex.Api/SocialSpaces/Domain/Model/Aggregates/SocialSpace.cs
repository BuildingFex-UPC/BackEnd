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
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private SocialSpace() { }

    public static SocialSpace Create(
        string externalId,
        int ownerAdminId,
        string name,
        string description,
        int? capacity,
        string imageUrl)
    {
        return new SocialSpace
        {
            ExternalId = externalId,
            OwnerAdminId = ownerAdminId,
            Name = name,
            Description = description,
            Capacity = capacity,
            ImageUrl = imageUrl,
        };
    }

    public void Patch(string name, string description, int? capacity, string? imageUrl)
    {
        Name = name;
        Description = description;
        Capacity = capacity;
        if (imageUrl is not null)
            ImageUrl = imageUrl;
    }
}
