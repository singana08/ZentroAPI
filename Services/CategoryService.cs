using HaluluAPI.Data;
using HaluluAPI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace HaluluAPI.Services;

/// <summary>
/// Service implementation for managing categories and subcategories
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(ApplicationDbContext dbContext, ILogger<CategoryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all categories with their active subcategories, ordered by name
    /// </summary>
    public async Task<IList<CategoryWithSubcategoriesDto>> GetAllCategoriesWithSubcategoriesAsync()
    {
        try
        {
            var categories = await _dbContext.Categories
                .Where(c => c.IsActive)
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new CategoryWithSubcategoriesDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Icon = c.Icon,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    Subcategories = c.Subcategories
                        .Where(s => s.IsActive)
                        .OrderBy(s => s.Name)
                        .Select(s => new SubcategoryDto
                        {
                            Id = s.Id,
                            Name = s.Name,
                            Description = s.Description,
                            Icon = s.Icon,
                            IsActive = s.IsActive,
                            CreatedAt = s.CreatedAt,
                            UpdatedAt = s.UpdatedAt
                        })
                        .ToList()
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {CategoryCount} categories with subcategories", categories.Count);
            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories with subcategories");
            throw;
        }
    }

    /// <summary>
    /// Retrieves a specific category with its active subcategories
    /// </summary>
    public async Task<CategoryWithSubcategoriesDto?> GetCategoryWithSubcategoriesAsync(int categoryId)
    {
        try
        {
            var category = await _dbContext.Categories
                .AsNoTracking()
                .Where(c => c.Id == categoryId)
                .Select(c => new CategoryWithSubcategoriesDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Icon = c.Icon,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    Subcategories = c.Subcategories
                        .Where(s => s.IsActive)
                        .OrderBy(s => s.Name)
                        .Select(s => new SubcategoryDto
                        {
                            Id = s.Id,
                            Name = s.Name,
                            Description = s.Description,
                            Icon = s.Icon,
                            IsActive = s.IsActive,
                            CreatedAt = s.CreatedAt,
                            UpdatedAt = s.UpdatedAt
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (category != null)
            {
                _logger.LogInformation("Retrieved category {CategoryId} with {SubcategoryCount} subcategories", 
                    categoryId, category.Subcategories.Count);
            }
            else
            {
                _logger.LogWarning("Category {CategoryId} not found", categoryId);
            }

            return category;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category {CategoryId} with subcategories", categoryId);
            throw;
        }
    }
}