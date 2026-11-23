using System.Security.Claims;
using BuildingFex.Api.Iam.Domain.Repositories;
using BuildingFex.Api.Notifications.Application.CommandServices;
using BuildingFex.Api.Notifications.Application.QueryServices;
using BuildingFex.Api.Notifications.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingFex.Api.Notifications.Interfaces.Rest.Compat;

[ApiController]
[Route("notifications")]
[Authorize]
public class NotificationsCompatController(
    INotificationQueryService queryService,
    INotificationCommandService commandService,
    IUserRepository userRepository) : ControllerBase
{
    private async Task<(string? userId, string? role)> ResolveUserAsync(CancellationToken ct)
    {
        var externalId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(externalId))
            return (null, null);

        var user = await userRepository.FindByExternalIdAsync(externalId, ct);
        return (user?.ExternalId ?? externalId, user?.Role);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? recipientId,
        [FromQuery] string? role,
        [FromQuery] bool? unreadOnly,
        CancellationToken ct)
    {
        var (authUserId, authRole) = await ResolveUserAsync(ct);
        var effectiveRecipient = string.IsNullOrWhiteSpace(recipientId) ? authUserId : recipientId;
        if (string.IsNullOrWhiteSpace(effectiveRecipient))
            return Ok(Array.Empty<object>());

        var effectiveRole = string.IsNullOrWhiteSpace(role) ? authRole : role;
        var notifications = await queryService.ListByRecipientAsync(effectiveRecipient, effectiveRole, unreadOnly, ct);
        return Ok(notifications.Select(NotificationResourceAssembler.ToResource));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(
        [FromQuery] string? recipientId,
        [FromQuery] string? role,
        CancellationToken ct)
    {
        var (authUserId, authRole) = await ResolveUserAsync(ct);
        var effectiveRecipient = string.IsNullOrWhiteSpace(recipientId) ? authUserId : recipientId;
        if (string.IsNullOrWhiteSpace(effectiveRecipient))
            return Ok(new { count = 0 });

        var effectiveRole = string.IsNullOrWhiteSpace(role) ? authRole : role;
        var count = await queryService.GetUnreadCountAsync(effectiveRecipient, effectiveRole, ct);
        return Ok(new { count });
    }

    [HttpPatch("{id}/read")]
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(string id, CancellationToken ct)
    {
        var success = await commandService.MarkAsReadAsync(id, ct);
        if (!success)
            return NotFound(new { code = "NOTIFICATION_NOT_FOUND", message = "Notificación no encontrada." });

        return Ok(new { success = true });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(
        [FromQuery] string? recipientId,
        [FromQuery] string? role,
        CancellationToken ct)
    {
        var (authUserId, authRole) = await ResolveUserAsync(ct);
        var effectiveRecipient = string.IsNullOrWhiteSpace(recipientId) ? authUserId : recipientId;
        if (string.IsNullOrWhiteSpace(effectiveRecipient))
            return Ok(new { markedCount = 0 });

        var effectiveRole = string.IsNullOrWhiteSpace(role) ? authRole : role;
        var count = await commandService.MarkAllAsReadAsync(effectiveRecipient, effectiveRole, ct);
        return Ok(new { markedCount = count });
    }
}

