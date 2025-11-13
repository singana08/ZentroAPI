using HaluluAPI.DTOs;
using HaluluAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HaluluAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AgreementController : ControllerBase
{
    private readonly IAgreementService _agreementService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AgreementController> _logger;

    public AgreementController(IAgreementService agreementService, ITokenService tokenService, ILogger<AgreementController> logger)
    {
        _agreementService = agreementService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Create agreement between requester and provider
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(AgreementResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateAgreement([FromBody] CreateAgreementDto request)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });

            var (success, message, data) = await _agreementService.CreateAgreementAsync(profileId.Value, request);
            if (!success)
                return BadRequest(new ErrorResponse { Message = message });

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating agreement");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Accept agreement (requester or provider)
    /// </summary>
    [HttpPost("accept")]
    [Authorize]
    [ProducesResponseType(typeof(AgreementResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AcceptAgreement([FromBody] AcceptAgreementDto request)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });

            var (success, message, data) = await _agreementService.AcceptAgreementAsync(profileId.Value, request);
            if (!success)
                return BadRequest(new ErrorResponse { Message = message });

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting agreement");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get agreement for a request
    /// </summary>
    [HttpGet("{requestId}")]
    [Authorize]
    [ProducesResponseType(typeof(AgreementResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAgreement([FromRoute] Guid requestId)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });

            var (success, message, data) = await _agreementService.GetAgreementAsync(requestId, profileId.Value);
            if (!success)
                return BadRequest(new ErrorResponse { Message = message });

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agreement");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Cancel agreement (requester or provider)
    /// </summary>
    [HttpPost("cancel")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CancelAgreement([FromBody] AcceptAgreementDto request)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });

            var (success, message) = await _agreementService.CancelAgreementAsync(profileId.Value, request);
            if (!success)
                return BadRequest(new ErrorResponse { Message = message });

            return Ok(new { Success = true, Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling agreement");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }
}