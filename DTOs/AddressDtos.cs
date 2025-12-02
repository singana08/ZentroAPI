using System.ComponentModel.DataAnnotations;

namespace ZentroAPI.DTOs;

public class CreateAddressDto
{
    public Guid ProfileId { get; set; } // Set automatically from JWT token
    
    [Required]
    [MaxLength(50)]
    public string Label { get; set; } = string.Empty;
    
    [Required]
    public double Latitude { get; set; }
    
    [Required]
    public double Longitude { get; set; }
    
    public string? AddressLine { get; set; }
    
    [MaxLength(100)]
    public string? City { get; set; }
    
    [MaxLength(100)]
    public string? State { get; set; }
    
    [MaxLength(20)]
    public string? PostalCode { get; set; }
    
    public bool IsPrimary { get; set; } = false;
}

public class UpdateAddressDto
{
    [Required]
    [MaxLength(50)]
    public string Label { get; set; } = string.Empty;
    
    [Required]
    public double Latitude { get; set; }
    
    [Required]
    public double Longitude { get; set; }
    
    public string? AddressLine { get; set; }
    
    [MaxLength(100)]
    public string? City { get; set; }
    
    [MaxLength(100)]
    public string? State { get; set; }
    
    [MaxLength(20)]
    public string? PostalCode { get; set; }
    
    public bool IsPrimary { get; set; } = false;
}

public class AddressResponseDto
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string Label { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
}
