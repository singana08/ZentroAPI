namespace ZentroAPI.Services;

/// <summary>
/// Service for handling file uploads
/// </summary>
public class FileUploadService : IFileUploadService
{
    private readonly IAzureBlobService _azureBlobService;
    private readonly ILogger<FileUploadService> _logger;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public FileUploadService(IAzureBlobService azureBlobService, ILogger<FileUploadService> logger)
    {
        _azureBlobService = azureBlobService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, string? FilePath)> UploadImageAsync(
        IFormFile file, string folder = "images")
    {
        var result = await _azureBlobService.UploadImageAsync(file, folder);
        return (result.Success, result.Message, result.BlobUrl);
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        return await _azureBlobService.DeleteBlobAsync(filePath);
    }

    public bool IsValidImageFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return false;

        if (file.Length > MaxFileSize)
            return false;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return _allowedExtensions.Contains(extension);
    }
}