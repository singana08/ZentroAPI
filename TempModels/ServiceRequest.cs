using System;
using System.Collections.Generic;

namespace ZentroAPI.TempModels;

public partial class ServiceRequest
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string BookingType { get; set; } = null!;

    public string MainCategory { get; set; } = null!;

    public string SubCategory { get; set; } = null!;

    public DateTime? Date { get; set; }

    public string? Time { get; set; }

    public string Location { get; set; } = null!;

    public string? Notes { get; set; }

    public string? AdditionalNotes { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
