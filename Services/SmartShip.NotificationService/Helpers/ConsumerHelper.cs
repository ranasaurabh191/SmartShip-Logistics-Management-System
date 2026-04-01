using SmartShip.NotificationService.DTOs;

namespace SmartShip.NotificationService.Helpers;

public static class ConsumerHelper
{
    public static async Task<string?> GetUserEmailAsync(
    IHttpClientFactory factory, ILogger logger, int userId, IConfiguration config)
    {
        try
        {
            var client = factory.CreateClient("IdentityService");

            var apiKey = config["InternalApi:ApiKey"];
            logger.LogInformation("DEBUG | ApiKey from config: '{ApiKey}'", apiKey);
            logger.LogInformation("DEBUG | Calling URL: api/admin/users/email/{UserId}", userId);

            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Add("X-Internal-Key", apiKey);
            }
            else
            {
                logger.LogWarning("DEBUG | ApiKey is NULL or EMPTY — check appsettings.json");
            }
            var response = await client.GetFromJsonAsync<UserEmailDto>($"api/admin/users/email/{userId}");

            if (response?.Email == null)
                throw new KeyNotFoundException($"Email not found for User {userId}.");

            return response.Email;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch email for User {UserId}", userId);
            return null;
        }
    }
 }