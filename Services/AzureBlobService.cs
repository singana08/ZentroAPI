using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ZentroAPI.Services;

public class AzureBlobService : IAzureBlobService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobService> _logger;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public AzureBlobService(BlobServiceClient blobServiceClient, ILogger<AzureBlobService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, string? BlobUrl)> UploadImageAsync(
        IFormFile file, string containerName = "images")
    {
        try
        {
            if (!IsValidImageFile(file))
            {
                return (false, "Invalid file type or size", null);
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var blobClient = containerClient.GetBlobClient(fileName);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = file.ContentType
            };

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            var blobUrl = blobClient.Uri.ToString();
            _logger.LogInformation("File uploaded to Azure Blob: {BlobUrl}", blobUrl);

            return (true, "File uploaded successfully", blobUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to Azure Blob Storage");
            return (false, "Error uploading file", null);
        }
    }

    public async Task<bool> DeleteBlobAsync(string blobUrl)
    {
        try
        {
            var uri = new Uri(blobUrl);
            var containerName = uri.Segments[1].TrimEnd('/');
            var blobName = uri.Segments[2];

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DeleteIfExistsAsync();
            
            if (response.Value)
            {
                _logger.LogInformation("Blob deleted: {BlobUrl}", blobUrl);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blob: {BlobUrl}", blobUrl);
            return false;
        }
    }

    private bool IsValidImageFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return false;

        if (file.Length > MaxFileSize)
            return false;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return _allowedExtensions.Contains(extension);
    }
}