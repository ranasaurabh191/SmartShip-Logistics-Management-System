using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace SmartShip.ShipmentService.Tests.Infrastructure;

public static class MockHttpContext
{
    public static IHttpContextAccessor WithUserId(int userId, string role = "CUSTOMER")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("userId", userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
        };
        context.Request.Headers["Authorization"] = "Bearer test-token";
        return new HttpContextAccessor { HttpContext = context };
    }

    public static IHttpContextAccessor Unauthenticated()
        => new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
}