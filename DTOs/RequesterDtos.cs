using System.ComponentModel.DataAnnotations;

namespace HaluluAPI.DTOs;

/// <summary>
/// DTO for creating a requester profile
/// </summary>
public class CreateRequesterDto
{
    // No additional fields needed for basic requester profile
}

/// <summary>
/// DTO for updating requester profile
/// </summary>
public class UpdateRequesterDto
{
    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO for returning requester profile information
/// </summary>
public class RequesterDto
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }



    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // User info
    public string? UserFullName { get; set; }

    public string? UserEmail { get; set; }

    public string? UserPhoneNumber { get; set; }

    public string? UserProfileImage { get; set; }
}

/// <summary>
/// DTO for requester profile with service requests count
/// </summary>
public class RequesterProfileDto : RequesterDto
{
    public int TotalServiceRequests { get; set; }

    public int ActiveRequests { get; set; }

    public int CompletedRequests { get; set; }

    public decimal AverageProviderRating { get; set; }
}