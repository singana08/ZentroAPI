using ZentroAPI.DTOs;
using ZentroAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ZentroAPI.Controllers;

/// <summary>
/// Controller for managing service categories and subcategories
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(
        ICategoryService categoryService,
        ILogger<CategoryController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all categories with their active subcategories, ordered by name
    /// </summary>
    /// <remarks>
    /// Returns a hierarchical structure with each category containing only its active subcategories.
    /// Both categories and subcategories are ordered alphabetically by name.
    /// </remarks>
    /// <returns>List of categories with associated active subcategories</returns>
    [HttpGet("with-subcategories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoryApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCategoriesWithSubcategories()
    {
        try
        {
            _logger.LogInformation("=== CategoryController: Starting GetCategoriesWithSubcategories ===");
            _logger.LogInformation("Fetching all categories with active subcategories");

            var categories = await _categoryService.GetAllCategoriesWithSubcategoriesAsync();
            _logger.LogInformation("=== CategoryService returned {Count} categories ===", categories?.Count ?? 0);

            var response = new CategoryApiResponse
            {
                Success = true,
                Message = $"Successfully retrieved {categories.Count} categories",
                Data = categories
            };

            _logger.LogInformation("Successfully retrieved {CategoryCount} categories with subcategories", 
                categories.Count);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories with subcategories");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Message = "An error occurred while retrieving categories"
            });
        }
    }



    /// <summary>
    /// Import categories and subcategories from categories-data.json file
    /// </summary>
    /// <returns>Import result with statistics</returns>
    [HttpPost("import")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoryImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ImportCategories()
    {
        try
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "categories-data.json");
            
            if (!System.IO.File.Exists(filePath))
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "categories-data.json file not found in project root"
                });
            }

            _logger.LogInformation("Starting category import from file: {FilePath}", filePath);

            var jsonContent = await System.IO.File.ReadAllTextAsync(filePath);

            var categories = System.Text.Json.JsonSerializer.Deserialize<List<ImportCategoryDto>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (categories == null || !categories.Any())
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "No valid categories found in the JSON file"
                });
            }

            var result = await _categoryService.ImportCategoriesAsync(categories);

            _logger.LogInformation("Category import completed: {Message}", result.Message);

            return Ok(result);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON format in categories-data.json");
            return BadRequest(new ErrorResponse
            {
                Message = "Invalid JSON format in categories-data.json file"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing categories from file");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Message = "An error occurred while importing categories"
            });
        }
    }
}
