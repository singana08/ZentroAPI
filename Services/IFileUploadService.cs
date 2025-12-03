namespace ZentroAPI.Services;

/// <summary>
/// Interface for file upload service
/// </summary>
public interface IFileUploadService
{
    Task<(bool Success, string Message, string? FilePath)> UploadImageAsync(
        IFormFile file, string folder = "uploads");
    
    Task<bool> DeleteFileAsync(string filePath);
    
    bool IsValidImageFile(IFormFile file);
}