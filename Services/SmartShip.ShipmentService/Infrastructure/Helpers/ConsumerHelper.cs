namespace SmartShip.ShipmentService.Infrastructure.Helpers;

public static class ConsumerHelper
{
    public static async Task<bool> ValidateCustomerExistsAsync(
        IHttpClientFactory factory, ILogger logger, int customerId, IConfiguration config)
    {
        try
        {
            var client = factory.CreateClient("IdentityService");

            var apiKey = config["InternalApi:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))  client.DefaultRequestHeaders.Add("X-Internal-Key", apiKey);

            var response = await client.GetAsync($"api/admin/users/exists/{customerId}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogWarning("Customer {CustomerId} not found in IdentityService.", customerId);
                return false;
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to validate customer {CustomerId}", customerId);
            return false;
        }
    }
}