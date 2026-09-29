using BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using BuildingFex.Api.Team.Domain.Model.Aggregates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildingFex.Api.Team.Interfaces.Rest.Compat;

[ApiController]
[Route("teamWorkers")]
[Authorize]
public class TeamWorkersCompatController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? ownerAdminId,
        CancellationToken ct)
    {
        var query = dbContext.Set<TeamWorker>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(ownerAdminId))
            query = query.Where(w => w.OwnerAdminId == ownerAdminId);
        var workers = await query.OrderByDescending(w => w.CreatedAt).ToListAsync(ct);
        return Ok(workers.Select(MapToResource));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTeamWorkerResource resource,
        CancellationToken ct)
    {
        var worker = TeamWorker.Create(
            resource.Id ?? $"worker-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            resource.OwnerAdminId ?? string.Empty,
            resource.Name ?? string.Empty,
            resource.Phone ?? string.Empty,
            resource.Dni ?? string.Empty,
            resource.Salary,
            resource.PhotoUrl ?? string.Empty);

        dbContext.Set<TeamWorker>().Add(worker);
        await dbContext.SaveChangesAsync(ct);
        return StatusCode(StatusCodes.Status201Created, MapToResource(worker));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var worker = await dbContext.Set<TeamWorker>()
            .FirstOrDefaultAsync(w => w.ExternalId == id || w.Id.ToString() == id, ct);
        if (worker is null) return NotFound();
        dbContext.Set<TeamWorker>().Remove(worker);
        await dbContext.SaveChangesAsync(ct);
        return NoContent();
    }

    private static object MapToResource(TeamWorker w) => new
    {
        id = w.ExternalId,
        ownerAdminId = w.OwnerAdminId,
        name = w.Name,
        phone = w.Phone,
        dni = w.Dni,
        salary = w.Salary,
        photoUrl = w.PhotoUrl,
    };
}

public class CreateTeamWorkerResource
{
    public string? Id { get; set; }
    public string? OwnerAdminId { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Dni { get; set; }
    public decimal Salary { get; set; }
    public string? PhotoUrl { get; set; }
}
