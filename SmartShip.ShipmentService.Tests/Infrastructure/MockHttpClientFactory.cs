using System.Net;
using System.Text;
using System.Text.Json;
using Moq;

namespace SmartShip.ShipmentService.Tests.Infrastructure;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;
    public MockHttpMessageHandler(HttpResponseMessage response) => _response = response;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => Task.FromResult(_response);
}

public static class MockHttpClientFactory
{
    public static IHttpClientFactory WithResponse(object responseBody, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(responseBody);
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var client = new HttpClient(new MockHttpMessageHandler(response)) { BaseAddress = new Uri("http://localhost/") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return factory.Object;
    }

    public static IHttpClientFactory WithNotFound()
    {
        var client = new HttpClient(new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound)))
        { BaseAddress = new Uri("http://localhost/") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return factory.Object;
    }
}