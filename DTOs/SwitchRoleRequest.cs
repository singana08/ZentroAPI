using System.ComponentModel.DataAnnotations;

namespace HaluluAPI.DTOs;

/// <summary>
/// DTO for switching user role
/// </summary>
public class SwitchRoleRequest
{
    [Required(ErrorMessage = "Role is required")]
    [RegularExpression("^(PROVIDER|REQUESTER)$", ErrorMessage = "Role must be either PROVIDER or REQUESTER")]
    public string Role { get; set; } = string.Empty;
}