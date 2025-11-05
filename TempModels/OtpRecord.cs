using System;
using System.Collections.Generic;

namespace HaluluAPI.TempModels;

public partial class OtpRecord
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string OtpCode { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public int AttemptCount { get; set; }

    public int MaxAttempts { get; set; }

    public bool IsLocked { get; set; }

    public Guid? UserId { get; set; }

    public int Purpose { get; set; }

    public virtual User? User { get; set; }
}
