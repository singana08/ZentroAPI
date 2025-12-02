using System;
using System.Collections.Generic;

namespace ZentroAPI.TempModels;

public partial class User
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? ProfileImage { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsProfileCompleted { get; set; }

    public string DefaultRole { get; set; } = null!;

    public string? Address { get; set; }

    public bool IsEmailVerified { get; set; }

    public bool IsPhoneVerified { get; set; }

    public virtual ICollection<OtpRecord> OtpRecords { get; set; } = new List<OtpRecord>();

    public virtual Provider? Provider { get; set; }

    public virtual Requester? Requester { get; set; }

    public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}
