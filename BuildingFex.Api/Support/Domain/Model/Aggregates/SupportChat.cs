namespace BuildingFex.Api.Support.Domain.Model.Aggregates;

public class SupportChat
{
    public int Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public string OwnerAdminId { get; private set; } = string.Empty;
    public string ResidentId { get; private set; } = string.Empty;
    public string ResidentName { get; private set; } = string.Empty;
    public string Topic { get; private set; } = string.Empty;
    public string Status { get; private set; } = "open";
    public string MessagesJson { get; private set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    private SupportChat() { }

    public static SupportChat Create(
        string externalId,
        string ownerAdminId,
        string residentId,
        string residentName,
        string topic)
    {
        var now = DateTimeOffset.UtcNow;
        return new SupportChat
        {
            ExternalId = externalId,
            OwnerAdminId = ownerAdminId,
            ResidentId = residentId,
            ResidentName = residentName,
            Topic = topic,
            Status = "open",
            MessagesJson = "[]",
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void UpdateMessages(string messagesJson)
    {
        MessagesJson = messagesJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateStatus(string status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
