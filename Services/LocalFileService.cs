namespace ZentroAPI.Services;

/// <summary>
/// Fallback local file service when Azure Blob Storage is not configured
/// </summary>
public class LocalFileService : IAzureBlobService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LocalFileService> _logger;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public LocalFileService(IWebHostEnvironment environment, ILogger<LocalFileService> logger)
    {
        _environment = environment;
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

            var uploadsFolder = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads", containerName);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            var relativePath = $"/uploads/{containerName}/{uniqueFileName}";
            _logger.LogInformation("File uploaded locally: {FilePath}", relativePath);

            return (true, "File uploaded successfully", relativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file locally");
            return (false, "Error uploading file", null);
        }
    }

    public Task<bool> DeleteBlobAsync(string blobUrl)
    {
        try
        {
            if (blobUrl.StartsWith("/uploads/"))
            {
                var fullPath = Path.Combine(_environment.WebRootPath ?? "wwwroot", blobUrl.TrimStart('/'));
                
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("File deleted locally: {FilePath}", blobUrl);
                    return Task.FromResult(true);
                }
            }
            
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting local file: {FilePath}", blobUrl);
            return Task.FromResult(false);
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