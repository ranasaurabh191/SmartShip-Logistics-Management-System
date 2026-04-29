using MassTransit;
using SmartShip.NotificationService.Core.Interfaces.Services;
using SmartShip.NotificationService.Infrastructure.Helpers;
using SmartShip.Shared.Events;

namespace SmartShip.NotificationService.Infrastructure.Messaging.Consumers;

public class UserCreatedConsumer : IConsumer<UserCreatedEvent>
{
    private readonly INotificationService _notification;
    private readonly ILogger<UserCreatedConsumer> _logger;

    public UserCreatedConsumer(INotificationService notification, ILogger<UserCreatedConsumer> logger)
    {
        _notification = notification;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("UserCreatedEvent received | UserId: {UserId}", msg.UserId);

        await _notification.SendAndSaveAsync(
            msg.UserId, msg.Email,
            type: "WelcomeEmail",
            subject: "Welcome to SmartShip!",
            body: EmailTemplates.WelcomeEmail(msg.Name, msg.Email, msg.Role)
        );
    }
}