namespace ZentroAPI.DTOs;

public class SearchProvidersRequest
{
    public string? Query { get; set; }
    public string? Category { get; set; }
    public string? Location { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? MaxDistance { get; set; } = 50; // km
    public decimal? MinRating { get; set; }
    public decimal? MaxPrice { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SearchServiceRequestsRequest
{
    public string? Query { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ProviderSearchResultDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfileImage { get; set; }
    public decimal Rating { get; set; }
    public int ExperienceYears { get; set; }
    public string? Bio { get; set; }
    public string? PricingModel { get; set; }
    public double? Distance { get; set; }
    public List<string> Categories { get; set; } = new();
}

public class ServiceRequestSearchResultDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MainCategory { get; set; } = string.Empty;
    public string SubCategory { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CategorySearchResultDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public List<SubcategorySearchResultDto> Subcategories { get; set; } = new();
}

public class SubcategorySearchResultDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}