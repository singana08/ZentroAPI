using HaluluAPI.DTOs;
using HaluluAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HaluluAPI.Controllers;

/// <summary>
/// Authentication controller handling OTP, verification, and registration
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Send OTP to email or phone address
    /// </summary>
    /// <param name="request">Email or phone to send OTP to</param>
    /// <returns>OTP sent response</returns>
    [HttpPost("send-otp")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SendOtpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        // Validate that at least one of email or phone is provided
        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Either email or phone number is required"
            });
        }

        // Validate email format if provided
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(request.Email);
            }
            catch
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Invalid email address format"
                });
            }
        }

        string identifier = !string.IsNullOrWhiteSpace(request.Email) ? request.Email : request.PhoneNumber!;
        _logger.LogInformation($"OTP request received for: {identifier}");

        var (success, message, expirationMinutes) = await _authService.SendOtpAsync(
            request.Email,
            request.PhoneNumber);

        if (!success)
        {
            return BadRequest(new ErrorResponse
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new SendOtpResponse
        {
            Success = true,
            Message = message,
            Email = request.Email ?? request.PhoneNumber ?? string.Empty,
            ExpirationMinutes = expirationMinutes
        });
    }

    /// <summary>
    /// Verify OTP and get JWT token
    /// </summary>
    /// <param name="request">Email/Phone and OTP code</param>
    /// <returns>JWT token and user information</returns>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VerifyOtpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        // Validate that at least one of email or phone is provided
        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Either email or phone number is required"
            });
        }

        // Validate OTP is provided
        if (string.IsNullOrWhiteSpace(request.OtpCode))
        {
            return BadRequest(new ErrorResponse
            {
                Message = "OTP is required"
            });
        }

        // Validate email format if provided
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(request.Email);
            }
            catch
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Invalid email address format"
                });
            }
        }

        string identifier = !string.IsNullOrWhiteSpace(request.Email) ? request.Email : request.PhoneNumber!;
        _logger.LogInformation($"OTP verification request for: {identifier}");

        var (success, message, token, user, isNewUser) = await _authService.VerifyOtpAsync(
            request.Email,
            request.PhoneNumber,
            request.OtpCode);

        if (!success)
        {
            return BadRequest(new ErrorResponse
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new VerifyOtpResponse
        {
            Success = true,
            Message = message,
            Token = token,
            User = user,
            IsNewUser = isNewUser
        });
    }

    /// <summary>
    /// Register/complete user profile and get JWT token
    /// </summary>
    /// <param name="request">User registration details</param>
    /// <returns>JWT token and updated user information</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Invalid request",
                Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .GroupBy(e => "validation")
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }

        _logger.LogInformation($"Registration request for: {request.FullName}, Role: {request.Role}");

        var (success, message, token, user) = await _authService.RegisterAsync(request);

        if (!success)
        {
            return BadRequest(new ErrorResponse
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new RegisterResponse
        {
            Success = true,
            Message = message,
            Token = token,
            User = user
        });
    }

    /// <summary>
    /// Get current user information (requires authentication)
    /// </summary>
    /// <returns>Current user information</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst("user_id")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ErrorResponse
            {
                Message = "User ID not found in token"
            });
        }

        _logger.LogInformation($"Getting user information for ID: {userId}");

        var user = await _authService.GetCurrentUserAsync(userId);

        if (user == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Message = "User not found"
            });
        }

        return Ok(user);
    }

    /// <summary>
    /// Get user by email (requires authentication)
    /// </summary>
    /// <param name="email">User email</param>
    /// <returns>User information</returns>
    [HttpGet("user/{email}")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Email is required"
            });
        }

        _logger.LogInformation($"Getting user information for email: {email}");

        var user = await _authService.GetUserByEmailAsync(email);

        if (user == null)
        {
            return NotFound(new ErrorResponse
            {
                Message = "User not found"
            });
        }

        return Ok(user);
    }

    /// <summary>
    /// Add additional profile (Provider or Requester)
    /// </summary>
    [HttpPost("add-profile")]
    [Authorize]
    public async Task<IActionResult> AddProfile([FromBody] AddProfileRequest request)
    {
        var userId = User.FindFirst("user_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ErrorResponse { Message = "Invalid token" });
        }

        var (success, message, token, user) = await _authService.AddProfileAsync(userId, request);
        
        if (!success)
        {
            return BadRequest(new ErrorResponse { Message = message });
        }

        return Ok(new RegisterResponse
        {
            Success = true,
            Message = message,
            Token = token,
            User = user
        });
    }

    /// <summary>
    /// Switch user role and get new token with appropriate profile ID
    /// </summary>
    [HttpPost("switch-role")]
    [Authorize]
    public async Task<IActionResult> SwitchRole([FromBody] SwitchRoleRequest request)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(new ErrorResponse { Message = "Invalid token" });
        }

        var (success, message, token, user) = await _authService.SwitchRoleAsync(userIdClaim, request.Role);
        
        if (!success)
        {
            return BadRequest(new ErrorResponse { Message = message });
        }

        return Ok(new RegisterResponse
        {
            Success = true,
            Message = message,
            Token = token,
            User = user
        });
    }



    /// <summary>
    /// Switch to different role and get new token
    /// </summary>
    [HttpPost("switch-to-role")]
    [Authorize]
    public async Task<IActionResult> SwitchToRole([FromBody] SwitchRoleRequest request)
    {
        var userId = User.FindFirst("user_id")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ErrorResponse { Message = "Invalid token" });
        }

        var (success, message, token, user) = await _authService.SwitchRoleAsync(userId, request.Role);
        
        if (!success)
        {
            return BadRequest(new ErrorResponse { Message = message });
        }

        return Ok(new { 
            Success = true,
            Message = message,
            Token = token,
            ActiveRole = request.Role,
            User = user
        });
    }

    /// <summary>
    /// Test authentication endpoint
    /// </summary>
    [HttpGet("test-auth")]
    [Authorize]
    public IActionResult TestAuth()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        return Ok(new { 
            IsAuthenticated = User.Identity?.IsAuthenticated,
            Claims = claims
        });
    }

    /// <summary>
    /// Test token parsing without authentication
    /// </summary>
    [HttpGet("test-token")]
    [AllowAnonymous]
    public IActionResult TestToken()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        return Ok(new { 
            AuthHeader = authHeader,
            HasBearer = authHeader?.StartsWith("Bearer ") == true,
            TokenLength = authHeader?.Replace("Bearer ", "").Length
        });
    }
}