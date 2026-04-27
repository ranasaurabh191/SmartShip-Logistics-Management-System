using SmartShip.TrackingService.Core.DTOs;

namespace SmartShip.TrackingService.Core.Interfaces.Services;

public interface IChatService
{
    Task<ChatResponseDto> ProcessAsync(ChatMessageRequest req, int userId, bool isAdmin);
}