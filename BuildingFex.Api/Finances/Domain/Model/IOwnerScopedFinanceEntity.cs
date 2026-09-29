namespace BuildingFex.Api.Finances.Domain.Model;

public interface IOwnerScopedFinanceEntity
{
    string ExternalId { get; }
    int OwnerAdminId { get; }
    string? ResidentExternalId { get; }
}
