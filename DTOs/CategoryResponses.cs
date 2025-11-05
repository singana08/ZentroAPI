namespace HaluluAPI.DTOs;

/// <summary>
/// Response DTO for subcategory details
/// </summary>
public class SubcategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Response DTO for category with associated subcategories
/// </summary>
public class CategoryWithSubcategoriesDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// List of active subcategories, ordered by name
    /// </summary>
    public IList<SubcategoryDto> Subcategories { get; set; } = [];
}

/// <summary>
/// Response wrapper for API endpoints
/// </summary>
public class CategoryApiResponse
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = "Successfully retrieved categories";
    public IList<CategoryWithSubcategoriesDto> Data { get; set; } = [];
}