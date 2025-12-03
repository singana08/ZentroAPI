using Microsoft.AspNetCore.Authorization;

namespace ZentroAPI.Attributes;

/// <summary>
/// Authorization attribute for role-based access control
/// </summary>
public class AuthorizeRoleAttribute : AuthorizeAttribute
{
    public AuthorizeRoleAttribute(params string[] roles)
    {
        Roles = string.Join(",", roles);
    }
}

/// <summary>
/// Common role constants
/// </summary>
public static class Roles
{
    public const string Requester = "Requester";
    public const string Provider = "Provider";
    public const string Admin = "Admin";
    public const string RequesterOrProvider = "Requester,Provider";
}