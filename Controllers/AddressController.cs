using HaluluAPI.DTOs;
using HaluluAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HaluluAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AddressController : ControllerBase
{
    private readonly IAddressService _addressService;
    private readonly ILogger<AddressController> _logger;

    public AddressController(IAddressService addressService, ILogger<AddressController> logger)
    {
        _addressService = addressService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new address
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(AddressResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAddress([FromBody] CreateAddressDto request)
    {
        var profileId = User.FindFirst("profile_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(profileId) || !Guid.TryParse(profileId, out var profileGuid))
        {
            return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });
        }

        // Override ProfileId from token
        request.ProfileId = profileGuid;
        
        var (success, message, address) = await _addressService.CreateAddressAsync(request);
        
        if (!success)
            return BadRequest(new ErrorResponse { Message = message });

        return CreatedAtAction(nameof(GetAddressById), new { id = address!.Id }, address);
    }

    /// <summary>
    /// Get all addresses for current profile
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(List<AddressResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfileAddresses()
    {
        var profileId = User.FindFirst("profile_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(profileId) || !Guid.TryParse(profileId, out var profileGuid))
        {
            return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });
        }

        var (success, message, addresses) = await _addressService.GetProfileAddressesAsync(profileGuid);
        
        if (!success)
            return BadRequest(new ErrorResponse { Message = message });

        return Ok(addresses);
    }

    /// <summary>
    /// Get a specific address by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(AddressResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAddressById(Guid id)
    {
        var profileId = User.FindFirst("profile_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(profileId) || !Guid.TryParse(profileId, out var profileGuid))
        {
            return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });
        }

        var (success, message, address) = await _addressService.GetAddressByIdAsync(id);
        
        if (!success)
            return NotFound(new ErrorResponse { Message = message });

        // Verify address belongs to current profile
        if (address!.ProfileId != profileGuid)
            return NotFound(new ErrorResponse { Message = "Address not found" });

        return Ok(address);
    }

    /// <summary>
    /// Update an address
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(AddressResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] UpdateAddressDto request)
    {
        var profileId = User.FindFirst("profile_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(profileId) || !Guid.TryParse(profileId, out var profileGuid))
        {
            return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });
        }

        // First check if address exists and belongs to current profile
        var (existsSuccess, _, existingAddress) = await _addressService.GetAddressByIdAsync(id);
        if (!existsSuccess || existingAddress!.ProfileId != profileGuid)
            return NotFound(new ErrorResponse { Message = "Address not found" });

        var (success, message, address) = await _addressService.UpdateAddressAsync(id, request);
        
        if (!success)
            return NotFound(new ErrorResponse { Message = message });

        return Ok(address);
    }

    /// <summary>
    /// Delete an address
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAddress(Guid id)
    {
        var profileId = User.FindFirst("profile_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(profileId) || !Guid.TryParse(profileId, out var profileGuid))
        {
            return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });
        }

        // First check if address exists and belongs to current profile
        var (existsSuccess, _, existingAddress) = await _addressService.GetAddressByIdAsync(id);
        if (!existsSuccess || existingAddress!.ProfileId != profileGuid)
            return NotFound(new ErrorResponse { Message = "Address not found" });

        var (success, message) = await _addressService.DeleteAddressAsync(id);
        
        if (!success)
            return NotFound(new ErrorResponse { Message = message });

        return Ok(new { Success = true, Message = message });
    }
}