namespace BuildingFex.Api.Shared.Domain.Model.Entities;

/// <summary>
/// Entidades con timestamps de auditoría (created_at, updated_at).
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset? CreatedAt { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
}
