using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentroAPI.Services;
using ZentroAPI.DTOs;

namespace ZentroAPI.Controllers;

/// <summary>
/// Controller for file upload operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FileUploadController : ControllerBase
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<FileUploadController> _logger;

    public FileUploadController(
        IFileUploadService fileUploadService,
        ILogger<FileUploadController> logger)
    {
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    /// <summary>
    /// Upload an image file
    /// </summary>
    /// <param name="file">Image file to upload</param>
    /// <param name="folder">Optional folder name (default: uploads)</param>
    /// <returns>Upload result with file path</returns>
    [HttpPost("image")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] string folder = "uploads")
    {
        if (file == null)
        {
            return BadRequest(new ErrorResponse { Message = "No file provided" });
        }

        var result = await _fileUploadService.UploadImageAsync(file, folder);

        if (result.Success)
        {
            return Ok(new
            {
                success = true,
                message = result.Message,
                filePath = result.FilePath,
                fileName = file.FileName,
                fileSize = file.Length,
                isAzureBlob = result.FilePath?.StartsWith("https://") == true
            });
        }

        return BadRequest(new ErrorResponse { Message = result.Message });
    }

    /// <summary>
    /// Delete an uploaded file
    /// </summary>
    /// <param name="filePath">Path of the file to delete</param>
    /// <returns>Deletion result</returns>
    [HttpDelete]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteFile([FromQuery] string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return BadRequest(new ErrorResponse { Message = "File path is required" });
        }

        var success = await _fileUploadService.DeleteFileAsync(filePath);

        if (success)
        {
            return Ok(new { success = true, message = "File deleted successfully" });
        }

        return BadRequest(new ErrorResponse { Message = "File not found or could not be deleted" });
    }
}