using System.ComponentModel.DataAnnotations;

namespace ZentroAPI.DTOs;

/// <summary>
/// DTO for adding additional profile (Provider or Requester)
/// </summary>
public class AddProfileRequest
{
    [Required(ErrorMessage = "Role is required (0 for REQUESTER, 1 for PROVIDER)")]
    public UserRoleDto Role { get; set; }

    // Provider-specific fields (required if Role is Provider)
    public string[]? ServiceCategories { get; set; }

    // Optional: Set as new default role
    public bool SetAsDefault { get; set; } = false;
}
