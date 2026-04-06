using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;

namespace SmartShip.PaymentService.Tests.Helpers;

public static class MockHttpContext
{
    public static IHttpContextAccessor WithUserId(int userId)
    {
        var claims = new List<Claim>
        {
            new Claim("userId", userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        httpContext.Request.Headers["Authorization"] = "Bearer test-token";

        var mock = new Mock<IHttpContextAccessor>();
        mock.Setup(x => x.HttpContext).Returns(httpContext);
        return mock.Object;
    }

    public static IHttpContextAccessor Unauthenticated()
    {
        var mock = new Mock<IHttpContextAccessor>();
        mock.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        return mock.Object;
    }
}