namespace ZentroAPI.DTOs;

public class SendMessageDto
{
    public Guid? RequestId { get; set; }  // Optional - can derive from QuoteId
    public Guid? QuoteId { get; set; }    // Primary - contains all context
    public Guid? ReceiverId { get; set; } // Optional - can derive from Quote
    public string MessageText { get; set; } = string.Empty;
}

public class MessageResponseDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public Guid RequestId { get; set; }
    public Guid? QuoteId { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsRead { get; set; }
    public bool IsDelivered { get; set; }
    public string? SenderName { get; set; }
    public bool IsOwn { get; set; }
}

public class MessagesListResponse
{
    public List<MessageResponseDto> Messages { get; set; } = new();
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public Guid? ProviderId { get; set; }
    public List<QuoteResponseDto> Quotes { get; set; } = new();
    public string RequestStatus { get; set; } = string.Empty;
}

public class AssignProviderDto
{
    public Guid RequestId { get; set; }
    public Guid ProviderId { get; set; }
}

public class RequestActionDto
{
    public Guid RequestId { get; set; }
    public string? Reason { get; set; }
}
