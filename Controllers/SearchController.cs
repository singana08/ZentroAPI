using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentroAPI.Services;
using ZentroAPI.DTOs;

namespace ZentroAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly IProviderMatchingService _matchingService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        ISearchService searchService,
        IProviderMatchingService matchingService,
        ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _matchingService = matchingService;
        _logger = logger;
    }

    [HttpPost("providers")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchProviders([FromBody] SearchProvidersRequest request)
    {
        var results = await _searchService.SearchProvidersAsync(request);
        return Ok(new { success = true, data = results, total = results.Count });
    }

    [HttpPost("service-requests")]
    [Authorize]
    public async Task<IActionResult> SearchServiceRequests([FromBody] SearchServiceRequestsRequest request)
    {
        var results = await _searchService.SearchServiceRequestsAsync(request);
        return Ok(new { success = true, data = results, total = results.Count });
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchCategories([FromQuery] string? query = null)
    {
        var results = await _searchService.SearchCategoriesAsync(query ?? "");
        return Ok(new { success = true, data = results, total = results.Count });
    }

    [HttpGet("recommendations")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRecommendedProviders(
        [FromQuery] string category,
        [FromQuery] double? latitude = null,
        [FromQuery] double? longitude = null,
        [FromQuery] int maxResults = 10)
    {
        var results = await _matchingService.GetRecommendedProvidersAsync(category, latitude, longitude, maxResults);
        return Ok(new { success = true, data = results, total = results.Count });
    }
}