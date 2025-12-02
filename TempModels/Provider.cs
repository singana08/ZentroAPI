using System;
using System.Collections.Generic;

namespace ZentroAPI.TempModels;

public partial class Provider
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string? ServiceCategories { get; set; }

    public int ExperienceYears { get; set; }

    public string? Bio { get; set; }

    public string? ServiceAreas { get; set; }

    public string? PricingModel { get; set; }

    public string? Documents { get; set; }

    public string? AvailabilitySlots { get; set; }

    public decimal Rating { get; set; }

    public decimal Earnings { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
