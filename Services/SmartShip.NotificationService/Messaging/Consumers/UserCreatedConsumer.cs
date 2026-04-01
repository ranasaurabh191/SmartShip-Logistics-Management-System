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
        var msg = context.Message;
        _logger.LogInformation("UserCreatedEvent received | UserId: {UserId}", msg.UserId);

        await _notification.SendAndSaveAsync(
            msg.UserId, msg.Email,
            type: "WelcomeEmail",
            subject: "Welcome to SmartShip! ",
            body: $"""
                <h2>Hi {msg.Name}, Welcome to SmartShip!</h2>
                <p>Your account has been created successfully.</p>
                <p><b>Email:</b> {msg.Email}</p>
                <p><b>Role:</b> {msg.Role}</p>
                <br/>
                <p>Start shipping today!</p>
                <p>— SmartShip Team</p>
            """
        );
    }
}