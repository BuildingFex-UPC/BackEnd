namespace BuildingFex.Api.Import.Domain.Model.Aggregates;

public class ImportUpload
{
    public int Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public string OwnerAdminId { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public string DataUrl { get; private set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }

    private ImportUpload() { }

    public static ImportUpload Create(
        string externalId,
        string ownerAdminId,
        string fileName,
        string mimeType,
        long size,
        string dataUrl)
    {
        return new ImportUpload
        {
            ExternalId = externalId,
            OwnerAdminId = ownerAdminId,
            FileName = fileName,
            MimeType = mimeType,
            Size = size,
            DataUrl = dataUrl,
            UploadedAt = DateTimeOffset.UtcNow,
        };
    }
}
