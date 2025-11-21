using HaluluAPI.Data;
using HaluluAPI.DTOs;
using HaluluAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HaluluAPI.Services;

/// <summary>
/// Service for managing reviews and ratings
/// </summary>
public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(ApplicationDbContext context, ILogger<ReviewService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, ReviewResponseDto? Data)> CreateReviewAsync(
        Guid customerId, 
        CreateReviewDto request)
    {
        try
        {
            // Verify service request exists and is completed
            var serviceRequest = await _context.ServiceRequests
                .Include(sr => sr.Requester)
                .ThenInclude(r => r!.User)
                .FirstOrDefaultAsync(sr => sr.Id == request.ServiceRequestId);

            if (serviceRequest == null)
            {
                return (false, "Service request not found", null);
            }

            if (serviceRequest.RequesterId != customerId)
            {
                return (false, "You can only review your own service requests", null);
            }

            if (serviceRequest.Status != ServiceRequestStatus.Completed)
            {
                return (false, "You can only review completed services", null);
            }

            if (serviceRequest.AssignedProviderId == null)
            {
                return (false, "No provider assigned to this service request", null);
            }

            // Check if review already exists
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ServiceRequestId == request.ServiceRequestId);

            if (existingReview != null)
            {
                return (false, "Review already exists for this service request", null);
            }

            // Create review
            var review = new Review
            {
                ServiceRequestId = request.ServiceRequestId,
                ProviderId = serviceRequest.AssignedProviderId.Value,
                CustomerId = customerId,
                Rating = request.Rating,
                Comment = request.Comment
            };

            _context.Reviews.Add(review);

            // Update provider's overall rating
            await UpdateProviderRatingAsync(serviceRequest.AssignedProviderId.Value);

            await _context.SaveChangesAsync();

            var responseDto = new ReviewResponseDto
            {
                Id = review.Id,
                CustomerName = serviceRequest.Requester?.User?.FullName ?? "Customer",
                ServiceType = $"{serviceRequest.SubCategory} - {serviceRequest.MainCategory}",
                Rating = review.Rating,
                Comment = review.Comment,
                Date = review.CreatedAt,
                ServiceRequestId = review.ServiceRequestId
            };

            return (true, "Review created successfully", responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review for service request {ServiceRequestId}", request.ServiceRequestId);
            return (false, "An error occurred while creating the review", null);
        }
    }

    public async Task<(bool Success, string Message, ProviderRatingsResponseDto? Data)> GetProviderRatingsAsync(
        Guid providerId)
    {
        try
        {
            // Verify provider exists
            var providerExists = await _context.Providers.AnyAsync(p => p.Id == providerId);
            if (!providerExists)
            {
                return (false, "Provider not found", null);
            }

            // Get all reviews for this provider
            var reviews = await _context.Reviews
                .Where(r => r.ProviderId == providerId)
                .Include(r => r.Customer)
                .ThenInclude(c => c!.User)
                .Include(r => r.ServiceRequest)
                .AsNoTracking()
                .ToListAsync();

            // Calculate summary
            var summary = CalculateRatingSummary(reviews);

            // Calculate rating breakdown
            var breakdown = CalculateRatingBreakdown(reviews);

            // Get recent reviews (last 10)
            var recentReviews = reviews
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .Select(r => new ReviewResponseDto
                {
                    Id = r.Id,
                    CustomerName = r.Customer?.User?.FullName ?? "Customer",
                    ServiceType = r.ServiceRequest != null 
                        ? $"{r.ServiceRequest.SubCategory} - {r.ServiceRequest.MainCategory}"
                        : "Service",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    Date = r.CreatedAt,
                    ServiceRequestId = r.ServiceRequestId
                })
                .ToList();

            var response = new ProviderRatingsResponseDto
            {
                Summary = summary,
                RatingBreakdown = breakdown,
                RecentReviews = recentReviews
            };

            return (true, "Provider ratings retrieved successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ratings for provider {ProviderId}", providerId);
            return (false, "An error occurred while retrieving ratings data", null);
        }
    }

    private RatingSummaryDto CalculateRatingSummary(List<Review> reviews)
    {
        if (!reviews.Any())
        {
            return new RatingSummaryDto
            {
                OverallRating = 0,
                TotalReviews = 0,
                RatingTrend = 0
            };
        }

        var overallRating = reviews.Average(r => r.Rating);
        var totalReviews = reviews.Count;

        // Calculate trend (current month vs previous month)
        var now = DateTime.UtcNow;
        var startOfCurrentMonth = new DateTime(now.Year, now.Month, 1);
        var startOfPreviousMonth = startOfCurrentMonth.AddMonths(-1);

        var currentMonthReviews = reviews.Where(r => r.CreatedAt >= startOfCurrentMonth).ToList();
        var previousMonthReviews = reviews.Where(r => r.CreatedAt >= startOfPreviousMonth && r.CreatedAt < startOfCurrentMonth).ToList();

        var currentMonthAvg = currentMonthReviews.Any() ? currentMonthReviews.Average(r => r.Rating) : 0;
        var previousMonthAvg = previousMonthReviews.Any() ? previousMonthReviews.Average(r => r.Rating) : 0;

        var ratingTrend = previousMonthAvg > 0 ? currentMonthAvg - previousMonthAvg : 0;

        return new RatingSummaryDto
        {
            OverallRating = Math.Round((decimal)overallRating, 1),
            TotalReviews = totalReviews,
            RatingTrend = Math.Round((decimal)ratingTrend, 1)
        };
    }

    private RatingBreakdownDto CalculateRatingBreakdown(List<Review> reviews)
    {
        var totalReviews = reviews.Count;
        if (totalReviews == 0)
        {
            return new RatingBreakdownDto();
        }

        var breakdown = new RatingBreakdownDto
        {
            FiveStar = CalculateStarRating(reviews, 5, totalReviews),
            FourStar = CalculateStarRating(reviews, 4, totalReviews),
            ThreeStar = CalculateStarRating(reviews, 3, totalReviews),
            TwoStar = CalculateStarRating(reviews, 2, totalReviews),
            OneStar = CalculateStarRating(reviews, 1, totalReviews)
        };

        return breakdown;
    }

    private StarRatingDto CalculateStarRating(List<Review> reviews, int starRating, int totalReviews)
    {
        var count = reviews.Count(r => r.Rating == starRating);
        var percentage = totalReviews > 0 ? (int)Math.Round((double)count / totalReviews * 100) : 0;

        return new StarRatingDto
        {
            Count = count,
            Percentage = percentage
        };
    }

    private async Task UpdateProviderRatingAsync(Guid providerId)
    {
        var provider = await _context.Providers.FindAsync(providerId);
        if (provider != null)
        {
            var averageRating = await _context.Reviews
                .Where(r => r.ProviderId == providerId)
                .AverageAsync(r => (double?)r.Rating) ?? 0;

            provider.Rating = (decimal)averageRating;
        }
    }
}