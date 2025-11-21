using System.ComponentModel.DataAnnotations;

namespace HaluluAPI.DTOs;

/// <summary>
/// DTO for submitting a review
/// </summary>
public class CreateReviewDto
{
    [Required]
    public Guid ServiceRequestId { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }

    [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
    public string? Comment { get; set; }
}

/// <summary>
/// DTO for review response
/// </summary>
public class ReviewResponseDto
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime Date { get; set; }
    public Guid ServiceRequestId { get; set; }
}

/// <summary>
/// DTO for provider ratings dashboard
/// </summary>
public class ProviderRatingsResponseDto
{
    public RatingSummaryDto Summary { get; set; } = new();
    public RatingBreakdownDto RatingBreakdown { get; set; } = new();
    public List<ReviewResponseDto> RecentReviews { get; set; } = new();
}

/// <summary>
/// Rating summary data
/// </summary>
public class RatingSummaryDto
{
    public decimal OverallRating { get; set; }
    public int TotalReviews { get; set; }
    public decimal RatingTrend { get; set; }
}

/// <summary>
/// Rating breakdown by stars
/// </summary>
public class RatingBreakdownDto
{
    public StarRatingDto FiveStar { get; set; } = new();
    public StarRatingDto FourStar { get; set; } = new();
    public StarRatingDto ThreeStar { get; set; } = new();
    public StarRatingDto TwoStar { get; set; } = new();
    public StarRatingDto OneStar { get; set; } = new();
}

/// <summary>
/// Individual star rating data
/// </summary>
public class StarRatingDto
{
    public int Count { get; set; }
    public int Percentage { get; set; }
}