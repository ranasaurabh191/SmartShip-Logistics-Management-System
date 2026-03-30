using MassTransit;
using SmartShip.NotificationService.Services;
using SmartShip.Shared.Events;

namespace SmartShip.NotificationService.Messaging.Consumers;

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
        var e = context.Message;
        _logger.LogInformation("UserCreatedEvent received | UserId: {UserId}", e.UserId);

        await _notification.SendAndSaveAsync(
            e.UserId, e.Email,
            type: "WelcomeEmail",
            subject: "Welcome to SmartShip! ",
            body: $"""
                <h2>Hi {e.Name}, Welcome to SmartShip!</h2>
                <p>Your account has been created successfully.</p>
                <p><b>Email:</b> {e.Email}</p>
                <p><b>Role:</b> {e.Role}</p>
                <br/>
                <p>Start shipping today!</p>
                <p>— SmartShip Team</p>
            """
        );
    }
}