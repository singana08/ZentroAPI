using ZentroAPI.DTOs;

namespace ZentroAPI.Services;

public interface ISearchService
{
    Task<List<ProviderSearchResultDto>> SearchProvidersAsync(SearchProvidersRequest request);
    Task<List<ServiceRequestSearchResultDto>> SearchServiceRequestsAsync(SearchServiceRequestsRequest request);
    Task<List<CategorySearchResultDto>> SearchCategoriesAsync(string query);
}