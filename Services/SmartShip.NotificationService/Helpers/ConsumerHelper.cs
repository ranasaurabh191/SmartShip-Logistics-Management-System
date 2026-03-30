using SmartShip.NotificationService.DTOs;

namespace SmartShip.NotificationService.Helpers;

public static class ConsumerHelper
{
    public static async Task<string?> GetUserEmailAsync(
        IHttpClientFactory factory, ILogger logger, int userId)
    {
        try
        {
            var client = factory.CreateClient("IdentityService");
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

    public static async Task<int?> GetCustomerIdAsync(
        IHttpClientFactory factory, ILogger logger, int shipmentId)
    {
        try
        {
            var client = factory.CreateClient("ShipmentService");
            var response = await client.GetFromJsonAsync<ShipmentCustomerDto>($"api/shipments/internal/{shipmentId}");

            if (response?.CustomerId == null)
                throw new KeyNotFoundException($"CustomerId not found for Shipment {shipmentId}.");

            return response.CustomerId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch CustomerId for Shipment {ShipmentId}", shipmentId);
            return null;  
        }
    }
}