using System.Text.Json;
using BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using BuildingFex.Api.Support.Domain.Model.Aggregates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildingFex.Api.Support.Interfaces.Rest.Compat;

[ApiController]
[Route("supportChats")]
[Authorize]
public class SupportChatsCompatController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? ownerAdminId,
        [FromQuery] string? residentId,
        CancellationToken ct)
    {
        var query = dbContext.Set<SupportChat>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(ownerAdminId))
            query = query.Where(c => c.OwnerAdminId == ownerAdminId);
        if (!string.IsNullOrWhiteSpace(residentId))
            query = query.Where(c => c.ResidentId == residentId);
        var chats = await query.OrderByDescending(c => c.UpdatedAt).ToListAsync(ct);
        return Ok(chats.Select(MapToResource));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var chat = await dbContext.Set<SupportChat>()
            .FirstOrDefaultAsync(c => c.ExternalId == id || c.Id.ToString() == id, ct);
        return chat is null ? NotFound() : Ok(MapToResource(chat));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupportChatResource resource, CancellationToken ct)
    {
        var chat = SupportChat.Create(
            resource.Id ?? $"support-chat-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            resource.OwnerAdminId ?? string.Empty,
            resource.ResidentId ?? string.Empty,
            resource.ResidentName ?? string.Empty,
            resource.Topic ?? "Soporte");

        dbContext.Set<SupportChat>().Add(chat);
        await dbContext.SaveChangesAsync(ct);
        return StatusCode(StatusCodes.Status201Created, MapToResource(chat));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateSupportChatResource resource,
        CancellationToken ct)
    {
        var chat = await dbContext.Set<SupportChat>()
            .FirstOrDefaultAsync(c => c.ExternalId == id || c.Id.ToString() == id, ct);
        if (chat is null) return NotFound();

        if (resource.Messages is not null)
            chat.UpdateMessages(JsonSerializer.Serialize(resource.Messages));
        if (!string.IsNullOrWhiteSpace(resource.Status))
            chat.UpdateStatus(resource.Status);
        if (!string.IsNullOrWhiteSpace(resource.UpdatedAt))
            chat.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(ct);
        return Ok(MapToResource(chat));
    }

    private static object MapToResource(SupportChat chat) => new
    {
        id = chat.ExternalId,
        ownerAdminId = chat.OwnerAdminId,
        residentId = chat.ResidentId,
        residentName = chat.ResidentName,
        topic = chat.Topic,
        status = chat.Status,
        messages = JsonSerializer.Deserialize<List<object>>(chat.MessagesJson) ?? new List<object>(),
        createdAt = chat.CreatedAt.ToString("o"),
        updatedAt = chat.UpdatedAt.ToString("o"),
    };
}

public class CreateSupportChatResource
{
    public string? Id { get; set; }
    public string? OwnerAdminId { get; set; }
    public string? ResidentId { get; set; }
    public string? ResidentName { get; set; }
    public string? Topic { get; set; }
}

public class UpdateSupportChatResource
{
    public List<JsonElement>? Messages { get; set; }
    public string? Status { get; set; }
    public string? UpdatedAt { get; set; }
}
