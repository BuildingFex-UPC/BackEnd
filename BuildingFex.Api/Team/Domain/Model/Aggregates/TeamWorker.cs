namespace BuildingFex.Api.Team.Domain.Model.Aggregates;

public class TeamWorker
{
    public int Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public string OwnerAdminId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Dni { get; private set; } = string.Empty;
    public decimal Salary { get; private set; }
    public string PhotoUrl { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    private TeamWorker() { }

    public static TeamWorker Create(
        string externalId,
        string ownerAdminId,
        string name,
        string phone,
        string dni,
        decimal salary,
        string photoUrl)
    {
        return new TeamWorker
        {
            ExternalId = externalId,
            OwnerAdminId = ownerAdminId,
            Name = name,
            Phone = phone,
            Dni = dni,
            Salary = salary,
            PhotoUrl = photoUrl,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
