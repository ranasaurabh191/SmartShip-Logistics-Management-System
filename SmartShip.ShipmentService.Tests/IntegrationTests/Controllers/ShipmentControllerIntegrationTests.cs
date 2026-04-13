using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using SmartShip.ShipmentService.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SmartShip.ShipmentService.Tests.IntegrationTests.Controllers;

public class ShipmentControllerIntegrationTests : IDisposable
{
    private readonly ShipmentServiceFactory _factory;

    public ShipmentControllerIntegrationTests()
    {
        _factory = new ShipmentServiceFactory("ShipmentTestDb_" + Guid.NewGuid());
    }

    public void Dispose() => _factory.Dispose();

    private HttpClient CreateClient()
        => _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    private HttpClient CreateAuthenticatedClient(int userId = 1, string role = "CUSTOMER")
    {
        var client = CreateClient();
        var token = TestJwtHelper.GenerateToken(userId, role);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }


    [Fact]
    public async Task POST_CreateShipment_WithoutToken_Returns401()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/shipments", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_MyShipments_WithoutToken_Returns401()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/shipments/my");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_ShipmentById_WithoutToken_Returns401()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/shipments/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_SchedulePickup_WithoutToken_Returns401()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/shipments/1/schedule-pickup", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PATCH_CancelShipment_WithoutToken_Returns401()
    {
        var client = CreateClient();
        var response = await client.PatchAsJsonAsync("/api/shipments/1/cancel", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_AdminShipments_WithoutToken_Returns401()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/admin/shipments");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PUT_AdminUpdateStatus_WithoutToken_Returns401()
    {
        var client = CreateClient();
        var response = await client.PutAsJsonAsync("/api/admin/shipments/status/1", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }


    [Fact]
    public async Task GET_AdminShipments_AsCustomer_Returns403()
    {
        var client = CreateAuthenticatedClient(1, "CUSTOMER");
        var response = await client.GetAsync("/api/admin/shipments");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PUT_AdminUpdateStatus_AsCustomer_Returns403()
    {
        var client = CreateAuthenticatedClient(1, "CUSTOMER");
        var response = await client.PutAsJsonAsync("/api/admin/shipments/status/1",
            new { Status = "InTransit" });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


    [Fact]
    public async Task GET_ShipmentById_NotFound_Returns404()
    {
        var client = CreateAuthenticatedClient(1);
        var response = await client.GetAsync("/api/shipments/99999");
        string body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, because: body);
    }

    [Fact]
    public async Task PATCH_CancelShipment_NotFound_Returns404()
    {
        var client = CreateAuthenticatedClient(1);
        var response = await client.PatchAsJsonAsync("/api/shipments/99999/cancel",
            new { Reason = "Test" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }


    [Fact]
    public async Task GET_Rate_RouteExists_DoesNotReturn404()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/shipments/rate?weight=2&type=Domestic");
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_MyShipments_WithToken_RouteExists()
    {
        var client = CreateAuthenticatedClient(1);
        var response = await client.GetAsync("/api/shipments/my");
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_AdminShipments_AsAdmin_RouteExists()
    {
        var client = CreateAuthenticatedClient(1, "ADMIN");
        var response = await client.GetAsync("/api/admin/shipments");
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }
}