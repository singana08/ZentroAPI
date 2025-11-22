namespace HaluluAPI.DTOs;

/// <summary>
/// DTO for importing subcategory data from JSON
/// </summary>
public class ImportSubcategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for importing category data from JSON
/// </summary>
public class ImportCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ImportSubcategoryDto> Subcategories { get; set; } = [];
}

/// <summary>
/// Response DTO for category import operation
/// </summary>
public class CategoryImportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CategoriesProcessed { get; set; }
    public int SubcategoriesProcessed { get; set; }
    public int CategoriesCreated { get; set; }
    public int CategoriesUpdated { get; set; }
    public int SubcategoriesCreated { get; set; }
    public int SubcategoriesUpdated { get; set; }
}