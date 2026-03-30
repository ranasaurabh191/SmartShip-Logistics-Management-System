namespace SmartShip.NotificationService.DTOs;

public record NotificationDto(
    int Id,
    int UserId,
    string Email,
    string Type,
    string Subject,
    bool IsEmailSent,
    string? ErrorMessage,
    string CreatedAt,
    string? SentAt
);

public class NotificationPagedRequest : PagedRequest
{
    public string? Type { get; set; }
    public bool? IsEmailSent { get; set; }
}

public class PagedRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? SortOrder { get; set; } = "desc";
}

public class PagedResponse<T>
{
    public IEnumerable<T> Data { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}