using ZentroAPI.DTOs;

namespace ZentroAPI.Services;

/// <summary>
/// Service for managing categories and subcategories
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Retrieves all categories with their active subcategories, ordered by name
    /// </summary>
    /// <returns>List of categories with associated active subcategories</returns>
    Task<IList<CategoryWithSubcategoriesDto>> GetAllCategoriesWithSubcategoriesAsync();

    /// <summary>
    /// Retrieves a specific category with its active subcategories
    /// </summary>
    /// <param name="categoryId">The category ID</param>
    /// <returns>Category with associated active subcategories, or null if not found</returns>
    Task<CategoryWithSubcategoriesDto?> GetCategoryWithSubcategoriesAsync(int categoryId);

    /// <summary>
    /// Imports categories and subcategories from JSON data
    /// </summary>
    /// <param name="categories">List of categories to import</param>
    /// <returns>Import result with statistics</returns>
    Task<CategoryImportResponse> ImportCategoriesAsync(List<ImportCategoryDto> categories);
}
