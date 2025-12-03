using Microsoft.EntityFrameworkCore;
using ZentroAPI.Data;
using ZentroAPI.DTOs;

namespace ZentroAPI.Services;

public class SearchService : ISearchService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SearchService> _logger;

    public SearchService(ApplicationDbContext context, ILogger<SearchService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ProviderSearchResultDto>> SearchProvidersAsync(SearchProvidersRequest request)
    {
        var query = _context.Providers
            .Include(p => p.User)
            .Where(p => p.IsActive);

        // Text search
        if (!string.IsNullOrEmpty(request.Query))
        {
            query = query.Where(p => 
                p.User.FullName.Contains(request.Query) ||
                (p.Bio != null && p.Bio.Contains(request.Query)));
        }

        // Rating filter
        if (request.MinRating.HasValue)
        {
            query = query.Where(p => p.Rating >= request.MinRating.Value);
        }

        var providers = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProviderSearchResultDto
            {
                Id = p.Id,
                FullName = p.User.FullName,
                ProfileImage = p.User.ProfileImage,
                Rating = p.Rating,
                ExperienceYears = p.ExperienceYears,
                Bio = p.Bio,
                PricingModel = p.PricingModel,
                Categories = new List<string>() // TODO: Add provider categories
            })
            .ToListAsync();

        // Calculate distance if coordinates provided
        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            foreach (var provider in providers)
            {
                // TODO: Calculate actual distance using provider addresses
                provider.Distance = new Random().NextDouble() * 50; // Placeholder
            }

            if (request.MaxDistance.HasValue)
            {
                providers = providers.Where(p => p.Distance <= request.MaxDistance.Value).ToList();
            }
        }

        return providers.OrderBy(p => p.Distance ?? 0).ThenByDescending(p => p.Rating).ToList();
    }

    public async Task<List<ServiceRequestSearchResultDto>> SearchServiceRequestsAsync(SearchServiceRequestsRequest request)
    {
        var query = _context.ServiceRequests.AsQueryable();

        // Text search
        if (!string.IsNullOrEmpty(request.Query))
        {
            query = query.Where(sr => 
                (sr.Title != null && sr.Title.Contains(request.Query)) ||
                (sr.Description != null && sr.Description.Contains(request.Query)) ||
                sr.MainCategory.Contains(request.Query) ||
                sr.SubCategory.Contains(request.Query));
        }

        // Category filter
        if (!string.IsNullOrEmpty(request.Category))
        {
            query = query.Where(sr => sr.MainCategory == request.Category);
        }

        // Status filter
        if (!string.IsNullOrEmpty(request.Status))
        {
            query = query.Where(sr => sr.Status.ToString() == request.Status);
        }

        // Date range filter
        if (request.FromDate.HasValue)
        {
            query = query.Where(sr => sr.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(sr => sr.CreatedAt <= request.ToDate.Value);
        }

        return await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(sr => new ServiceRequestSearchResultDto
            {
                Id = sr.Id,
                Title = sr.Title ?? "",
                Description = sr.Description ?? "",
                MainCategory = sr.MainCategory,
                SubCategory = sr.SubCategory,
                Location = sr.Location,
                Status = sr.Status.ToString(),
                CreatedAt = sr.CreatedAt
            })
            .OrderByDescending(sr => sr.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<CategorySearchResultDto>> SearchCategoriesAsync(string query)
    {
        return await _context.Categories
            .Include(c => c.Subcategories)
            .Where(c => c.IsActive && 
                (string.IsNullOrEmpty(query) || 
                 c.Name.Contains(query) || 
                 (c.Description != null && c.Description.Contains(query))))
            .Select(c => new CategorySearchResultDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Icon = c.Icon,
                Subcategories = c.Subcategories
                    .Where(sc => sc.IsActive)
                    .Select(sc => new SubcategorySearchResultDto
                    {
                        Id = sc.Id,
                        Name = sc.Name,
                        Description = sc.Description
                    })
                    .ToList()
            })
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}