namespace SmartShip.NotificationService.Domain.Entities;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsEmailSent { get; set; } = false;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? SentAt { get; set; }
}