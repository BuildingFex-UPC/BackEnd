using BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using BuildingFex.Api.Import.Domain.Model.Aggregates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildingFex.Api.Import.Interfaces.Rest.Compat;

[ApiController]
[Route("importUploads")]
[Authorize]
public class ImportUploadsCompatController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? ownerAdminId,
        CancellationToken ct)
    {
        var query = dbContext.Set<ImportUpload>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(ownerAdminId))
            query = query.Where(u => u.OwnerAdminId == ownerAdminId);
        var items = await query.OrderByDescending(u => u.UploadedAt).ToListAsync(ct);
        return Ok(items.Select(MapToResource));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateImportUploadResource resource,
        CancellationToken ct)
    {
        var upload = ImportUpload.Create(
            resource.Id ?? $"import-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            resource.OwnerAdminId ?? string.Empty,
            resource.FileName ?? "archivo",
            resource.MimeType ?? "application/octet-stream",
            resource.Size,
            resource.DataUrl ?? string.Empty);

        dbContext.Set<ImportUpload>().Add(upload);
        await dbContext.SaveChangesAsync(ct);
        return StatusCode(StatusCodes.Status201Created, MapToResource(upload));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var upload = await dbContext.Set<ImportUpload>()
            .FirstOrDefaultAsync(u => u.ExternalId == id || u.Id.ToString() == id, ct);
        if (upload is null) return NotFound();
        dbContext.Set<ImportUpload>().Remove(upload);
        await dbContext.SaveChangesAsync(ct);
        return NoContent();
    }

    private static object MapToResource(ImportUpload u) => new
    {
        id = u.ExternalId,
        ownerAdminId = u.OwnerAdminId,
        fileName = u.FileName,
        mimeType = u.MimeType,
        size = u.Size,
        uploadedAt = u.UploadedAt.ToString("o"),
        dataUrl = u.DataUrl,
    };
}

public class CreateImportUploadResource
{
    public string? Id { get; set; }
    public string? OwnerAdminId { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public long Size { get; set; }
    public string? DataUrl { get; set; }
}
