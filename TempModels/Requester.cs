using System;
using System.Collections.Generic;

namespace ZentroAPI.TempModels;

public partial class Requester
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string? Address { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
