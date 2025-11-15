namespace HaluluAPI.DTOs;

public class CreateQuoteDto
{
    public Guid RequestId { get; set; }
    public decimal Price { get; set; }
    public string? Message { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class QuoteResponseDto
{
    public Guid Id { get; set; }
    public Guid ProviderId { get; set; }
    public Guid RequestId { get; set; }
    public decimal Price { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string QuoteStatus { get; set; } = "Pending";
    public DateTime UpdatedAt { get; set; }
    public string? ProviderName { get; set; }
    public decimal? ProviderRating { get; set; }
}

public class QuotesListResponse
{
    public List<QuoteResponseDto> Quotes { get; set; } = new();
    public int TotalCount { get; set; }
}