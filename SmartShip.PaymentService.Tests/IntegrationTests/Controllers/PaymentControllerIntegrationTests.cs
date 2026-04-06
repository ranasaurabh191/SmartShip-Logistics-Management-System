using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartShip.PaymentService.Core.DTOs;
using SmartShip.PaymentService.Domain.Entities.Enums;
using SmartShip.PaymentService.Infrastructure.Data;
using SmartShip.PaymentService.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;

namespace SmartShip.PaymentService.Tests.IntegrationTests.Controllers;

public class PaymentControllerIntegrationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public PaymentControllerIntegrationTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                var toRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<PaymentDbContext>) ||
                    d.ServiceType == typeof(PaymentDbContext) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)) ||
                    d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true ||
                    d.ImplementationType?.FullName?.Contains("SqlServer") == true
                ).ToList();
                foreach (var d in toRemove) services.Remove(d);

                services.AddDbContext<PaymentDbContext>(options =>
                {
                    options.UseInMemoryDatabase("PaymentTestDb_" + Guid.NewGuid());
                    options.ConfigureWarnings(w =>
                        w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                },
                ServiceLifetime.Scoped,
                ServiceLifetime.Singleton);

                var massTransitDescriptors = services
                    .Where(d =>
                        (d.ServiceType.Namespace?.StartsWith("MassTransit") == true) ||
                        (d.ImplementationType?.Namespace?.StartsWith("MassTransit") == true))
                    .ToList();
                foreach (var d in massTransitDescriptors) services.Remove(d);

                services.AddMassTransit(x =>
                    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context)));

                var rabbitConn = services.FirstOrDefault(d =>
                    d.ServiceType == typeof(RabbitMQ.Client.IConnection));
                if (rabbitConn != null) services.Remove(rabbitConn);

                var healthChecks = services.Where(d =>
                    d.ServiceType.FullName?.Contains("HealthChecks") == true ||
                    d.ImplementationType?.FullName?.Contains("HealthChecks") == true ||
                    d.ServiceType.Name.Contains("IHealthCheck")
                ).ToList();
                foreach (var d in healthChecks) services.Remove(d);
                services.AddHealthChecks();
            });
        });
    }

    public void Dispose() => _factory.Dispose();

    private HttpClient CreateAuthenticatedClient(int userId = 29, string role = "Customer")
    {
        var client = _factory.CreateClient();
        var token = TestJwtHelper.GenerateToken(userId, role);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    [Fact]
    public async Task POST_CreateOrder_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/payment/create-order",
            new CreateOrderRequest(1, PaymentMethod.Online));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_Verify_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/payment/verify",
            new VerifyPaymentRequest { RazorpayOrderId = "order_MOCK_123", ShipmentId = 1 });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_ShipmentPayment_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/payment/shipment/26");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_PaymentStatus_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync(
            "/api/payment/payment-status?razorpayOrderId=order_MOCK_123");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_ShipmentPayment_NotFound_Returns404()
    {
        var response = await CreateAuthenticatedClient().GetAsync("/api/payment/shipment/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_PaymentStatus_ByOrderId_NotFound_Returns404()
    {
        var response = await CreateAuthenticatedClient().GetAsync(
            "/api/payment/payment-status?razorpayOrderId=order_DOESNOTEXIST");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_PaymentStatus_ByShipmentId_NotFound_Returns404()
    {
        var response = await CreateAuthenticatedClient().GetAsync(
            "/api/payment/payment-status?shipmentId=99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_CreateOrder_RouteExists_DoesNotReturn404()
    {
        var response = await CreateAuthenticatedClient().PostAsJsonAsync("/api/payment/create-order",
            new CreateOrderRequest(99999, PaymentMethod.Online));
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound,
            because: "route /api/payment/create-order must be registered");
    }

}