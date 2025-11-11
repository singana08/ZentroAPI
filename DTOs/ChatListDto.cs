namespace HaluluAPI.DTOs;

public class ChatListItemDto
{
    public Guid RequestId { get; set; }
    public Guid OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string ServiceTitle { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageTime { get; set; }
    public int UnreadCount { get; set; }
    public bool IsOnline { get; set; }
}

public class ChatListResponse
{
    public List<ChatListItemDto> Chats { get; set; } = new();
    public int TotalCount { get; set; }
}