using HaluluAPI.DTOs;
using HaluluAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HaluluAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MessageController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<MessageController> _logger;

    public MessageController(IMessageService messageService, ITokenService tokenService, ILogger<MessageController> logger)
    {
        _messageService = messageService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Send a message in a service request chat
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto request)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });

            var (success, message, data) = await _messageService.SendMessageAsync(profileId.Value, request);
            if (!success)
                return BadRequest(new ErrorResponse { Message = message });

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get chat messages for a service request
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(MessagesListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMessages([FromQuery] Guid requestId)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });

            var (success, message, data) = await _messageService.GetMessagesAsync(requestId, profileId.Value);
            if (!success)
                return BadRequest(new ErrorResponse { Message = message });

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Assign provider to request
    /// </summary>
    [HttpPost("assign")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AssignProvider([FromBody] AssignProviderDto request)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });

            var (success, message) = await _messageService.AssignProviderAsync(profileId.Value, request);
            if (!success)
                return BadRequest(new ErrorResponse { Message = message });

            return Ok(new { Success = true, Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning provider");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Reject a service request
    /// </summary>
    [HttpPost("reject")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RejectRequest([FromBody] RequestActionDto request)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });

            var (success, message) = await _messageService.RejectRequestAsync(profileId.Value, request);
            if (!success)
                return BadRequest(new ErrorResponse { Message = message });

            return Ok(new { Success = true, Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting request");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Reopen a service request
    /// </summary>
    [HttpPost("reopen")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReopenRequest([FromBody] RequestActionDto request)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });

            var (success, message) = await _messageService.ReopenRequestAsync(profileId.Value, request);
            if (!success)
                return BadRequest(new ErrorResponse { Message = message });

            return Ok(new { Success = true, Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reopening request");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get chat list - all conversations like WhatsApp
    /// </summary>
    [HttpGet("chats")]
    [Authorize]
    [ProducesResponseType(typeof(ChatListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetChatList()
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });

            var (success, message, data) = await _messageService.GetChatListAsync(profileId.Value);
            if (!success)
                return BadRequest(new ErrorResponse { Message = message });

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat list");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }
}