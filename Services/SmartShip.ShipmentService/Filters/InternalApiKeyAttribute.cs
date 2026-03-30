using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SmartShip.ShipmentService.Filters;

public class InternalApiKeyAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedKey = config["InternalApi:ApiKey"];

        context.HttpContext.Request.Headers.TryGetValue("X-Internal-Key", out var receivedKey);

        if (string.IsNullOrEmpty(receivedKey) || receivedKey != expectedKey)
        {
            context.Result = new UnauthorizedResult();
        }
    }
}