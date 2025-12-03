namespace ZentroAPI.Services;

public interface IAzureBlobService
{
    Task<(bool Success, string Message, string? BlobUrl)> UploadImageAsync(
        IFormFile file, string containerName = "images");
    
    Task<bool> DeleteBlobAsync(string blobUrl);
}