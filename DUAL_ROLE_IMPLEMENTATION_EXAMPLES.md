# Dual-Role Implementation - Code Examples

This document provides ready-to-use code examples for implementing the dual-role architecture.

---

## 📦 Step 1: Create New Models

### ProviderService.cs

```csharp
using System.ComponentModel.DataAnnotations;

namespace HaluluAPI.Models;

/// <summary>
/// Represents a service offered by a provider
/// </summary>
public class ProviderService
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProviderId { get; set; }

    [Required]
    [StringLength(100)]
    public string MainCategory { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string SubCategory { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public decimal PricePerHour { get; set; }

    [StringLength(50)]
    public string? PricingType { get; set; } = "hourly"; // hourly, fixed, negotiable

    [StringLength(500)]
    public string? ServiceAreas { get; set; } // JSON array: ["Area1", "Area2"]

    public bool IsActive { get; set; } = true;

    public double? AverageRating { get; set; } = 0;

    public int TotalReviews { get; set; } = 0;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public User? Provider { get; set; }
}
```

### ProviderAvailability.cs

```csharp
using System.ComponentModel.DataAnnotations;

namespace HaluluAPI.Models;

/// <summary>
/// Represents provider's available working hours
/// </summary>
public class ProviderAvailability
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProviderId { get; set; }

    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; } // e.g., 09:00:00

    [Required]
    public TimeSpan EndTime { get; set; } // e.g., 18:00:00

    public bool IsAvailable { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public User? Provider { get; set; }
}
```

### ServiceBid.cs

```csharp
using System.ComponentModel.DataAnnotations;

namespace HaluluAPI.Models;

/// <summary>
/// Represents a provider's bid/quote for a service request
/// </summary>
public class ServiceBid
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid RequestId { get; set; }

    [Required]
    public Guid ProviderId { get; set; }

    [Required]
    public decimal QuoteAmount { get; set; }

    [StringLength(1000)]
    public string? QuoteDescription { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ServiceRequest? ServiceRequest { get; set; }
    public User? Provider { get; set; }
}
```

### Review.cs

```csharp
using System.ComponentModel.DataAnnotations;

namespace HaluluAPI.Models;

/// <summary>
/// Represents a review/rating for a provider
/// </summary>
public class Review
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ServiceRequestId { get; set; }

    [Required]
    public Guid ProviderId { get; set; }

    [Required]
    public Guid ReviewedByUserId { get; set; } // Requester who gave the review

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; } // 1-5 stars

    [StringLength(1000)]
    public string? Comment { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ServiceRequest? ServiceRequest { get; set; }
    public User? Provider { get; set; }
    public User? ReviewedByUser { get; set; }
}
```

---

## 🗄️ Step 2: Update ApplicationDbContext

```csharp
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<OtpRecord> OtpRecords { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Subcategory> Subcategories { get; set; }
    public DbSet<ServiceRequest> ServiceRequests { get; set; }
    
    // NEW
    public DbSet<ProviderService> ProviderServices { get; set; }
    public DbSet<ProviderAvailability> ProviderAvailabilities { get; set; }
    public DbSet<ServiceBid> ServiceBids { get; set; }
    public DbSet<Review> Reviews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Existing configurations...

        // ProviderService configurations
        modelBuilder.Entity<ProviderService>()
            .HasOne(ps => ps.Provider)
            .WithMany()
            .HasForeignKey(ps => ps.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProviderService>()
            .HasIndex(ps => ps.ProviderId);

        modelBuilder.Entity<ProviderService>()
            .HasIndex(ps => ps.MainCategory);

        // ProviderAvailability configurations
        modelBuilder.Entity<ProviderAvailability>()
            .HasOne(pa => pa.Provider)
            .WithMany()
            .HasForeignKey(pa => pa.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProviderAvailability>()
            .HasIndex(pa => pa.ProviderId);

        // ServiceBid configurations
        modelBuilder.Entity<ServiceBid>()
            .HasOne(sb => sb.ServiceRequest)
            .WithMany()
            .HasForeignKey(sb => sb.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ServiceBid>()
            .HasOne(sb => sb.Provider)
            .WithMany()
            .HasForeignKey(sb => sb.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ServiceBid>()
            .HasIndex(sb => sb.RequestId);

        modelBuilder.Entity<ServiceBid>()
            .HasIndex(sb => sb.ProviderId);

        // Review configurations
        modelBuilder.Entity<Review>()
            .HasOne(r => r.ServiceRequest)
            .WithMany()
            .HasForeignKey(r => r.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Provider)
            .WithMany()
            .HasForeignKey(r => r.ProviderId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.ReviewedByUser)
            .WithMany()
            .HasForeignKey(r => r.ReviewedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Review>()
            .HasIndex(r => r.ProviderId);

        // Update ServiceRequest with provider fields
        modelBuilder.Entity<ServiceRequest>()
            .Property(sr => sr.AssignedProviderId)
            .IsRequired(false);

        modelBuilder.Entity<ServiceRequest>()
            .HasOne(sr => sr.AssignedProvider)
            .WithMany()
            .HasForeignKey(sr => sr.AssignedProviderId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ServiceRequest>()
            .HasIndex(sr => sr.AssignedProviderId);
    }
}
```

---

## 🎯 Step 3: Update ServiceRequest Model

Add these properties to the existing `ServiceRequest.cs`:

```csharp
// Add to ServiceRequest class:

/// <summary>
/// Provider assigned to fulfill this request
/// </summary>
public Guid? AssignedProviderId { get; set; }

/// <summary>
/// Provider's quote for this request
/// </summary>
[StringLength(500)]
public string? ProviderQuote { get; set; }

/// <summary>
/// When the provider accepted this request
/// </summary>
public DateTime? AcceptedAt { get; set; }

/// <summary>
/// When the service was completed
/// </summary>
public DateTime? CompletedAt { get; set; }

/// <summary>
/// Reason for cancellation (if cancelled)
/// </summary>
[StringLength(500)]
public string? CancellationReason { get; set; }

/// <summary>
/// Rating given by requester (1-5)
/// </summary>
[Range(1, 5)]
public int? ProviderRating { get; set; }

/// <summary>
/// Review comment for the provider
/// </summary>
[StringLength(1000)]
public string? ProviderReview { get; set; }

/// <summary>
/// Navigation property for assigned provider
/// </summary>
public User? AssignedProvider { get; set; }
```

---

## 📝 Step 4: Create DTOs

### ProviderServiceDtos.cs

```csharp
using System.ComponentModel.DataAnnotations;

namespace HaluluAPI.DTOs;

/// <summary>
/// DTO for creating a provider service
/// </summary>
public class CreateProviderServiceDto
{
    [Required]
    [StringLength(100)]
    public string MainCategory { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string SubCategory { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal PricePerHour { get; set; }

    [StringLength(50)]
    public string? PricingType { get; set; } = "hourly";

    [StringLength(500)]
    public string? ServiceAreas { get; set; }
}

/// <summary>
/// DTO for updating a provider service
/// </summary>
public class UpdateProviderServiceDto
{
    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal? PricePerHour { get; set; }

    [StringLength(50)]
    public string? PricingType { get; set; }

    [StringLength(500)]
    public string? ServiceAreas { get; set; }

    public bool? IsActive { get; set; }
}

/// <summary>
/// Response DTO for provider service
/// </summary>
public class ProviderServiceResponseDto
{
    public Guid Id { get; set; }

    public Guid ProviderId { get; set; }

    public string MainCategory { get; set; } = string.Empty;

    public string SubCategory { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal PricePerHour { get; set; }

    public string? PricingType { get; set; }

    public string? ServiceAreas { get; set; }

    public bool IsActive { get; set; }

    public double? AverageRating { get; set; }

    public int TotalReviews { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
```

### ServiceBidDtos.cs

```csharp
using System.ComponentModel.DataAnnotations;

namespace HaluluAPI.DTOs;

/// <summary>
/// DTO for submitting a bid/quote
/// </summary>
public class CreateServiceBidDto
{
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal QuoteAmount { get; set; }

    [StringLength(1000)]
    public string? QuoteDescription { get; set; }
}

/// <summary>
/// Response DTO for service bid
/// </summary>
public class ServiceBidResponseDto
{
    public Guid Id { get; set; }

    public Guid RequestId { get; set; }

    public Guid ProviderId { get; set; }

    public string ProviderName { get; set; } = string.Empty;

    public double? ProviderRating { get; set; }

    public int ProviderReviewCount { get; set; }

    public decimal QuoteAmount { get; set; }

    public string? QuoteDescription { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
```

### ReviewDtos.cs

```csharp
using System.ComponentModel.DataAnnotations;

namespace HaluluAPI.DTOs;

/// <summary>
/// DTO for submitting a review
/// </summary>
public class CreateReviewDto
{
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}

/// <summary>
/// Response DTO for review
/// </summary>
public class ReviewResponseDto
{
    public Guid Id { get; set; }

    public Guid ServiceRequestId { get; set; }

    public Guid ProviderId { get; set; }

    public string ReviewerName { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }
}
```

---

## 🔧 Step 5: Create Provider Service Interface & Implementation

### IProviderService.cs

```csharp
using HaluluAPI.DTOs;

namespace HaluluAPI.Services;

public interface IProviderService
{
    // Provider Services Management
    Task<(bool Success, string Message, ProviderServiceResponseDto? Data)> CreateProviderServiceAsync(
        Guid providerId, CreateProviderServiceDto request);

    Task<(bool Success, string Message, ProviderServiceResponseDto? Data)> GetProviderServiceAsync(
        Guid serviceId, Guid providerId);

    Task<(bool Success, string Message, List<ProviderServiceResponseDto> Data)> GetProviderServicesAsync(
        Guid providerId, int page = 1, int pageSize = 10);

    Task<(bool Success, string Message, ProviderServiceResponseDto? Data)> UpdateProviderServiceAsync(
        Guid serviceId, Guid providerId, UpdateProviderServiceDto request);

    Task<(bool Success, string Message)> DeleteProviderServiceAsync(
        Guid serviceId, Guid providerId);

    // Availability Management
    Task<(bool Success, string Message)> SetProviderAvailabilityAsync(
        Guid providerId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime);

    Task<(bool Success, string Message, List<object> Data)> GetProviderAvailabilityAsync(
        Guid providerId);

    // Matching & Discovery
    Task<(bool Success, string Message, List<ServiceRequestResponseDto> Data)> GetAvailableRequestsAsync(
        Guid providerId, int page = 1, int pageSize = 10);
}
```

### ProviderService.cs Implementation

```csharp
using HaluluAPI.Data;
using HaluluAPI.DTOs;
using HaluluAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HaluluAPI.Services;

public class ProviderServiceImpl : IProviderService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ProviderServiceImpl> _logger;

    public ProviderServiceImpl(
        ApplicationDbContext dbContext,
        ILogger<ProviderServiceImpl> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Create a new service offering
    /// </summary>
    public async Task<(bool Success, string Message, ProviderServiceResponseDto? Data)> 
        CreateProviderServiceAsync(Guid providerId, CreateProviderServiceDto request)
    {
        try
        {
            // Validate provider exists
            var provider = await _dbContext.Users.FindAsync(providerId);
            if (provider == null)
            {
                _logger.LogWarning($"Provider {providerId} not found");
                return (false, "Provider not found", null);
            }

            // Validate provider role
            if (provider.Role != UserRole.Provider)
            {
                _logger.LogWarning($"User {providerId} is not a provider");
                return (false, "User must have provider role", null);
            }

            var providerService = new ProviderService
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                MainCategory = request.MainCategory.Trim(),
                SubCategory = request.SubCategory.Trim(),
                Description = request.Description?.Trim(),
                PricePerHour = request.PricePerHour,
                PricingType = request.PricingType ?? "hourly",
                ServiceAreas = request.ServiceAreas,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.ProviderServices.Add(providerService);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Provider service created: {providerService.Id}");

            return (true, "Service created successfully", MapToResponseDto(providerService));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating provider service: {ex.Message}");
            return (false, "Error creating service", null);
        }
    }

    /// <summary>
    /// Get provider's available requests to bid on
    /// </summary>
    public async Task<(bool Success, string Message, List<ServiceRequestResponseDto> Data)> 
        GetAvailableRequestsAsync(Guid providerId, int page = 1, int pageSize = 10)
    {
        try
        {
            // Get provider's categories
            var providerServices = await _dbContext.ProviderServices
                .Where(ps => ps.ProviderId == providerId && ps.IsActive)
                .Select(ps => ps.MainCategory)
                .Distinct()
                .ToListAsync();

            if (!providerServices.Any())
            {
                return (true, "No services configured", new List<ServiceRequestResponseDto>());
            }

            // Get available requests matching provider's categories
            var availableRequests = await _dbContext.ServiceRequests
                .Where(sr => providerServices.Contains(sr.MainCategory) 
                    && sr.Status == ServiceRequestStatus.Pending
                    && sr.AssignedProviderId == null
                    && sr.UserId != providerId) // Don't show own requests
                .OrderByDescending(sr => sr.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = availableRequests.Select(sr => new ServiceRequestResponseDto
            {
                Id = sr.Id,
                UserId = sr.UserId,
                BookingType = sr.BookingType.ToString(),
                MainCategory = sr.MainCategory,
                SubCategory = sr.SubCategory,
                Date = sr.Date,
                Time = sr.Time,
                Location = sr.Location,
                Notes = sr.Notes,
                Status = sr.Status.ToString(),
                CreatedAt = sr.CreatedAt,
                UpdatedAt = sr.UpdatedAt
            }).ToList();

            return (true, "Available requests retrieved", dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting available requests: {ex.Message}");
            return (false, "Error retrieving requests", new List<ServiceRequestResponseDto>());
        }
    }

    /// <summary>
    /// Get provider's services
    /// </summary>
    public async Task<(bool Success, string Message, List<ProviderServiceResponseDto> Data)> 
        GetProviderServicesAsync(Guid providerId, int page = 1, int pageSize = 10)
    {
        try
        {
            var services = await _dbContext.ProviderServices
                .Where(ps => ps.ProviderId == providerId)
                .OrderByDescending(ps => ps.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = services.Select(MapToResponseDto).ToList();
            return (true, "Services retrieved", dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting provider services: {ex.Message}");
            return (false, "Error retrieving services", new List<ProviderServiceResponseDto>());
        }
    }

    /// <summary>
    /// Set provider availability for a specific day
    /// </summary>
    public async Task<(bool Success, string Message)> 
        SetProviderAvailabilityAsync(Guid providerId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime)
    {
        try
        {
            // Remove existing availability for this day
            var existing = await _dbContext.ProviderAvailabilities
                .Where(pa => pa.ProviderId == providerId && pa.DayOfWeek == dayOfWeek)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                _dbContext.ProviderAvailabilities.Remove(existing);
            }

            // Add new availability
            var availability = new ProviderAvailability
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                DayOfWeek = dayOfWeek,
                StartTime = startTime,
                EndTime = endTime,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.ProviderAvailabilities.Add(availability);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Availability set for provider {providerId} on {dayOfWeek}");
            return (true, "Availability updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error setting availability: {ex.Message}");
            return (false, "Error updating availability");
        }
    }

    /// <summary>
    /// Get provider's availability schedule
    /// </summary>
    public async Task<(bool Success, string Message, List<object> Data)> 
        GetProviderAvailabilityAsync(Guid providerId)
    {
        try
        {
            var availability = await _dbContext.ProviderAvailabilities
                .Where(pa => pa.ProviderId == providerId)
                .OrderBy(pa => pa.DayOfWeek)
                .Select(pa => new
                {
                    dayOfWeek = pa.DayOfWeek.ToString(),
                    startTime = pa.StartTime,
                    endTime = pa.EndTime,
                    isAvailable = pa.IsAvailable
                })
                .Cast<object>()
                .ToListAsync();

            return (true, "Availability retrieved", availability);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting availability: {ex.Message}");
            return (false, "Error retrieving availability", new List<object>());
        }
    }

    /// <summary>
    /// Get a single provider service
    /// </summary>
    public async Task<(bool Success, string Message, ProviderServiceResponseDto? Data)> 
        GetProviderServiceAsync(Guid serviceId, Guid providerId)
    {
        try
        {
            var service = await _dbContext.ProviderServices
                .FirstOrDefaultAsync(ps => ps.Id == serviceId && ps.ProviderId == providerId);

            if (service == null)
                return (false, "Service not found", null);

            return (true, "Service retrieved", MapToResponseDto(service));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting provider service: {ex.Message}");
            return (false, "Error retrieving service", null);
        }
    }

    /// <summary>
    /// Update provider service
    /// </summary>
    public async Task<(bool Success, string Message, ProviderServiceResponseDto? Data)> 
        UpdateProviderServiceAsync(Guid serviceId, Guid providerId, UpdateProviderServiceDto request)
    {
        try
        {
            var service = await _dbContext.ProviderServices
                .FirstOrDefaultAsync(ps => ps.Id == serviceId && ps.ProviderId == providerId);

            if (service == null)
                return (false, "Service not found", null);

            if (request.Description != null)
                service.Description = request.Description.Trim();

            if (request.PricePerHour.HasValue)
                service.PricePerHour = request.PricePerHour.Value;

            if (request.PricingType != null)
                service.PricingType = request.PricingType;

            if (request.ServiceAreas != null)
                service.ServiceAreas = request.ServiceAreas;

            if (request.IsActive.HasValue)
                service.IsActive = request.IsActive.Value;

            service.UpdatedAt = DateTime.UtcNow;

            _dbContext.ProviderServices.Update(service);
            await _dbContext.SaveChangesAsync();

            return (true, "Service updated successfully", MapToResponseDto(service));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating provider service: {ex.Message}");
            return (false, "Error updating service", null);
        }
    }

    /// <summary>
    /// Delete provider service
    /// </summary>
    public async Task<(bool Success, string Message)> 
        DeleteProviderServiceAsync(Guid serviceId, Guid providerId)
    {
        try
        {
            var service = await _dbContext.ProviderServices
                .FirstOrDefaultAsync(ps => ps.Id == serviceId && ps.ProviderId == providerId);

            if (service == null)
                return (false, "Service not found");

            _dbContext.ProviderServices.Remove(service);
            await _dbContext.SaveChangesAsync();

            return (true, "Service deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting provider service: {ex.Message}");
            return (false, "Error deleting service");
        }
    }

    // Helper method
    private static ProviderServiceResponseDto MapToResponseDto(ProviderService service)
    {
        return new ProviderServiceResponseDto
        {
            Id = service.Id,
            ProviderId = service.ProviderId,
            MainCategory = service.MainCategory,
            SubCategory = service.SubCategory,
            Description = service.Description,
            PricePerHour = service.PricePerHour,
            PricingType = service.PricingType,
            ServiceAreas = service.ServiceAreas,
            IsActive = service.IsActive,
            AverageRating = service.AverageRating,
            TotalReviews = service.TotalReviews,
            CreatedAt = service.CreatedAt,
            UpdatedAt = service.UpdatedAt
        };
    }
}
```

---

## 🎮 Step 6: Update ServiceRequestService with Role-Based Logic

Add this method to `IServiceRequestService`:

```csharp
/// <summary>
/// Get requests based on user role:
/// - Requester: Returns requests created by user
/// - Provider: Returns requests assigned to user
/// </summary>
Task<(bool Success, string Message, PaginatedServiceRequestsDto? Data)> 
    GetDashboardRequestsAsync(Guid userId, int page, int pageSize, 
        string? status, string? bookingType, string? category);
```

Add implementation to `ServiceRequestService`:

```csharp
/// <summary>
/// Role-based dashboard: returns different data based on user's role
/// </summary>
public async Task<(bool Success, string Message, PaginatedServiceRequestsDto? Data)> 
    GetDashboardRequestsAsync(Guid userId, int page, int pageSize, 
        string? status, string? bookingType, string? category)
{
    try
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
            return (false, "User not found", null);

        IQueryable<ServiceRequest> query;

        if (user.Role == UserRole.Requester)
        {
            // Requester: Show requests they created
            query = _dbContext.ServiceRequests
                .Where(sr => sr.UserId == userId);
        }
        else if (user.Role == UserRole.Provider)
        {
            // Provider: Show requests assigned to them
            query = _dbContext.ServiceRequests
                .Where(sr => sr.AssignedProviderId == userId);
        }
        else
        {
            return (false, "Invalid user role", null);
        }

        // Apply filters
        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<ServiceRequestStatus>(status, ignoreCase: true, out var statusEnum))
                query = query.Where(sr => sr.Status == statusEnum);
        }

        if (!string.IsNullOrEmpty(bookingType))
        {
            if (Enum.TryParse<BookingType>(bookingType, ignoreCase: true, out var bookingEnum))
                query = query.Where(sr => sr.BookingType == bookingEnum);
        }

        if (!string.IsNullOrEmpty(category))
            query = query.Where(sr => sr.MainCategory == category);

        var total = await query.CountAsync();
        var requests = await query
            .OrderByDescending(sr => sr.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = requests.Select(MapToResponseDto).ToList();

        var result = new PaginatedServiceRequestsDto
        {
            Data = dtos,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return (true, "Dashboard data retrieved", result);
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error getting dashboard requests: {ex.Message}");
        return (false, "Error retrieving dashboard data", null);
    }
}
```

---

## 🎮 Step 7: Create ProviderServiceController

```csharp
using HaluluAPI.DTOs;
using HaluluAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HaluluAPI.Controllers;

/// <summary>
/// Controller for managing provider services and availability
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProviderServiceController : ControllerBase
{
    private readonly IProviderService _providerService;
    private readonly ILogger<ProviderServiceController> _logger;

    public ProviderServiceController(
        IProviderService providerService,
        ILogger<ProviderServiceController> logger)
    {
        _providerService = providerService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new service offering
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ProviderServiceResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateProviderService([FromBody] CreateProviderServiceDto request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new ErrorResponse { Success = false, Message = "Authentication failed" });

            var (success, message, data) = await _providerService.CreateProviderServiceAsync(userId, request);

            if (!success)
                return BadRequest(new ErrorResponse { Success = false, Message = message });

            return CreatedAtAction(nameof(GetProviderService), new { id = data?.Id }, data);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating provider service: {ex.Message}");
            return StatusCode(500, new ErrorResponse 
            { 
                Success = false, 
                Message = "Error creating service" 
            });
        }
    }

    /// <summary>
    /// Get all provider's services
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(List<ProviderServiceResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviderServices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new ErrorResponse { Success = false, Message = "Authentication failed" });

            var (success, message, data) = await _providerService.GetProviderServicesAsync(userId, page, pageSize);

            if (!success)
                return StatusCode(500, new ErrorResponse { Success = false, Message = message });

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving services: {ex.Message}");
            return StatusCode(500, new ErrorResponse { Success = false, Message = "Error retrieving services" });
        }
    }

    /// <summary>
    /// Get a specific provider service
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ProviderServiceResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviderService([FromRoute] Guid id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new ErrorResponse { Success = false, Message = "Authentication failed" });

            var (success, message, data) = await _providerService.GetProviderServiceAsync(id, userId);

            if (!success)
                return NotFound(new ErrorResponse { Success = false, Message = message });

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving service: {ex.Message}");
            return StatusCode(500, new ErrorResponse { Success = false, Message = "Error retrieving service" });
        }
    }

    /// <summary>
    /// Update a provider service
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ProviderServiceResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProviderService(
        [FromRoute] Guid id,
        [FromBody] UpdateProviderServiceDto request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new ErrorResponse { Success = false, Message = "Authentication failed" });

            var (success, message, data) = await _providerService.UpdateProviderServiceAsync(id, userId, request);

            if (!success)
                return BadRequest(new ErrorResponse { Success = false, Message = message });

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating service: {ex.Message}");
            return StatusCode(500, new ErrorResponse { Success = false, Message = "Error updating service" });
        }
    }

    /// <summary>
    /// Set provider availability for a day of week
    /// </summary>
    [HttpPost("availability")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetAvailability(
        [FromBody] SetAvailabilityDto request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new ErrorResponse { Success = false, Message = "Authentication failed" });

            var (success, message) = await _providerService.SetProviderAvailabilityAsync(
                userId, 
                request.DayOfWeek, 
                request.StartTime, 
                request.EndTime);

            if (!success)
                return BadRequest(new ErrorResponse { Success = false, Message = message });

            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error setting availability: {ex.Message}");
            return StatusCode(500, new ErrorResponse { Success = false, Message = "Error setting availability" });
        }
    }

    /// <summary>
    /// Get provider's availability schedule
    /// </summary>
    [HttpGet("availability")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailability()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new ErrorResponse { Success = false, Message = "Authentication failed" });

            var (success, message, data) = await _providerService.GetProviderAvailabilityAsync(userId);

            if (!success)
                return StatusCode(500, new ErrorResponse { Success = false, Message = message });

            return Ok(new { data });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving availability: {ex.Message}");
            return StatusCode(500, new ErrorResponse { Success = false, Message = "Error retrieving availability" });
        }
    }

    /// <summary>
    /// Get available requests for this provider
    /// </summary>
    [HttpGet("requests/available")]
    [Authorize]
    [ProducesResponseType(typeof(PaginatedServiceRequestsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new ErrorResponse { Success = false, Message = "Authentication failed" });

            var (success, message, data) = await _providerService.GetAvailableRequestsAsync(userId, page, pageSize);

            if (!success)
                return StatusCode(500, new ErrorResponse { Success = false, Message = message });

            return Ok(new { data });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving available requests: {ex.Message}");
            return StatusCode(500, new ErrorResponse { Success = false, Message = "Error retrieving requests" });
        }
    }
}

/// <summary>
/// DTO for setting provider availability
/// </summary>
public class SetAvailabilityDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
```

---

## 🔐 Step 8: Secure the Endpoints

Add authorization attributes and owner checks:

```csharp
// Example: Only allow provider to update their own services
[Authorize]
[HttpPut("ProviderService/{id}")]
public async Task<IActionResult> UpdateProviderService(Guid id, [FromBody] UpdateProviderServiceDto dto)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    // Authorization check: Service must belong to this user
    var (success, _, data) = await _providerService.GetProviderServiceAsync(id, Guid.Parse(userId));
    
    if (!success)
        return Forbid("You can only update your own services");
    
    // Proceed with update...
}
```

---

## ✅ Database Migrations

Create migrations in order:

```powershell
# Step 1: Add provider-related tables
dotnet ef migrations add AddProviderServicesTables

# Step 2: Add bidding system
dotnet ef migrations add AddServiceBiddingTables

# Step 3: Add review system
dotnet ef migrations add AddReviewSystem

# Step 4: Update ServiceRequest with provider fields
dotnet ef migrations add AddProviderFieldsToServiceRequest

# Apply all migrations
dotnet ef database update
```

---

## 🎯 Quick Test Examples

### Register as Provider

```json
POST /api/Auth/register
{
  "email": "provider@example.com",
  "phone": "+919876543210",
  "password": "SecurePass123!",
  "firstName": "John",
  "lastName": "Cleaner",
  "role": "Provider"
}
```

### Create Service Offering

```json
POST /api/ProviderService
{
  "mainCategory": "Cleaning Services",
  "subCategory": "Deep Cleaning",
  "description": "Professional deep cleaning services",
  "pricePerHour": 500.00,
  "pricingType": "hourly",
  "serviceAreas": "[\"Mumbai\", \"Pune\"]"
}
```

### Set Availability

```json
POST /api/ProviderService/availability
{
  "dayOfWeek": 1,  // Monday = 1
  "startTime": "09:00:00",
  "endTime": "18:00:00"
}
```

### Get Available Requests

```
GET /api/ProviderService/requests/available?page=1&pageSize=10
```

---

This completes the dual-role implementation! 🚀