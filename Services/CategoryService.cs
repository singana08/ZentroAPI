using HaluluAPI.Data;
using HaluluAPI.DTOs;
using HaluluAPI.Models;
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

    /// <summary>
    /// Imports categories and subcategories from JSON data
    /// </summary>
    public async Task<CategoryImportResponse> ImportCategoriesAsync(List<ImportCategoryDto> categories)
    {
        var response = new CategoryImportResponse();
        
        try
        {
            foreach (var categoryDto in categories)
            {
                response.CategoriesProcessed++;
                
                // Check if category exists
                var existingCategory = await _dbContext.Categories
                    .FirstOrDefaultAsync(c => c.Name == categoryDto.Name);
                
                Category category;
                if (existingCategory == null)
                {
                    // Create new category
                    category = new Category
                    {
                        Name = categoryDto.Name,
                        Description = categoryDto.Description,
                        Icon = categoryDto.Icon,
                        IsActive = categoryDto.IsActive,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbContext.Categories.Add(category);
                    response.CategoriesCreated++;
                }
                else
                {
                    // Update existing category
                    existingCategory.Description = categoryDto.Description;
                    existingCategory.Icon = categoryDto.Icon;
                    existingCategory.IsActive = categoryDto.IsActive;
                    existingCategory.UpdatedAt = DateTime.UtcNow;
                    category = existingCategory;
                    response.CategoriesUpdated++;
                }
                
                await _dbContext.SaveChangesAsync();
                
                // Process subcategories
                foreach (var subcategoryDto in categoryDto.Subcategories)
                {
                    response.SubcategoriesProcessed++;
                    
                    var existingSubcategory = await _dbContext.Subcategories
                        .FirstOrDefaultAsync(s => s.Name == subcategoryDto.Name && s.CategoryId == category.Id);
                    
                    if (existingSubcategory == null)
                    {
                        // Create new subcategory
                        var subcategory = new Subcategory
                        {
                            Name = subcategoryDto.Name,
                            Description = subcategoryDto.Description,
                            Icon = subcategoryDto.Icon,
                            IsActive = subcategoryDto.IsActive,
                            CategoryId = category.Id,
                            CreatedAt = DateTime.UtcNow
                        };
                        _dbContext.Subcategories.Add(subcategory);
                        response.SubcategoriesCreated++;
                    }
                    else
                    {
                        // Update existing subcategory
                        existingSubcategory.Description = subcategoryDto.Description;
                        existingSubcategory.Icon = subcategoryDto.Icon;
                        existingSubcategory.IsActive = subcategoryDto.IsActive;
                        existingSubcategory.UpdatedAt = DateTime.UtcNow;
                        response.SubcategoriesUpdated++;
                    }
                }
            }
            
            await _dbContext.SaveChangesAsync();
            
            response.Success = true;
            response.Message = "Categories imported successfully";
            
            _logger.LogInformation("Imported {CategoriesCreated} new categories, updated {CategoriesUpdated}, created {SubcategoriesCreated} subcategories, updated {SubcategoriesUpdated}",
                response.CategoriesCreated, response.CategoriesUpdated, response.SubcategoriesCreated, response.SubcategoriesUpdated);
                
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing categories");
            response.Success = false;
            response.Message = $"Error importing categories: {ex.Message}";
            return response;
        }
    }
}