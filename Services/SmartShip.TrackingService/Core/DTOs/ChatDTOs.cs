namespace SmartShip.TrackingService.Core.DTOs;

public class ChatMessageRequest
{
    public string Message { get; set; } = string.Empty;
    public int? ShipmentId { get; set; }
    public int? SelectedShipmentId { get; set; }
    public List<ChatHistoryItem>? History { get; set; }
}

public class ChatHistoryItem
{
    public string Role { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

    
public class ChatResponseDto
{
    public string Reply { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public object? Data { get; set; }
    public List<ShipmentChip>? ShipmentChips { get; set; } 

    public ChatResponseDto() { }
    public ChatResponseDto(string reply, string intent, object? data,
        List<ShipmentChip>? chips = null)
    {
        Reply = reply;
        Intent = intent;
        Data = data;
        ShipmentChips = chips;
    }
}

public class ShipmentChip
{
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty; 
    public string Status { get; set; } = string.Empty;
}

public class ShipmentSummary
{
    public int Id { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string ShipmentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal WeightKg { get; set; }
    public string OriginCity { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}