using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using Serilog;
using SmartShip.NotificationService.API.Middleware;
using SmartShip.NotificationService.Core.Interfaces.Persistence;
using SmartShip.NotificationService.Core.Interfaces.Repositories;
using SmartShip.NotificationService.Core.Interfaces.Services;
using SmartShip.NotificationService.Core.Services;
using SmartShip.NotificationService.Infrastructure.Data;
using SmartShip.NotificationService.Infrastructure.Messaging.Consumers;
using SmartShip.NotificationService.Infrastructure.Persistence;
using SmartShip.NotificationService.Infrastructure.Repositories;
using SmartShip.NotificationService.Infrastructure.Services;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information(" --> Starting NotificationService...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "NotificationService")  
        .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName));

    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
                return new BadRequestObjectResult(new { message = "Validation failed.", errors });
            };
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Notification Service",
            Version = "v1"
        });
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter your token."
        });
        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddDbContext<NotificationDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    var urls = builder.Configuration.GetSection("ServiceUrls");
    builder.Services.AddHttpClient("IdentityService", c =>
        c.BaseAddress = new Uri(urls["IdentityService"]!));
    builder.Services.AddHttpClient("ShipmentService", c =>
        c.BaseAddress = new Uri(urls["ShipmentService"]!));

    var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";

    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<UserCreatedConsumer>();
        x.AddConsumer<ShipmentCreatedConsumer>();
        x.AddConsumer<ShipmentStatusUpdatedConsumer>();
        x.AddConsumer<ShipmentDeliveredConsumer>();
        x.AddConsumer<ShipmentCancelledConsumer>();
        x.AddConsumer<PaymentCompletedConsumer>();
        x.AddConsumer<PaymentRefundedConsumer>();
        x.AddConsumer<PaymentFailedConsumer>();

        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host(rabbitHost, "/", h =>
            {
                h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
                h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
            });
            cfg.ReceiveEndpoint("notification-user-created", e => e.ConfigureConsumer<UserCreatedConsumer>(ctx));
            cfg.ReceiveEndpoint("notification-shipment-created", e => e.ConfigureConsumer<ShipmentCreatedConsumer>(ctx));
            cfg.ReceiveEndpoint("notification-status-updated", e => e.ConfigureConsumer<ShipmentStatusUpdatedConsumer>(ctx));
            cfg.ReceiveEndpoint("notification-shipment-delivered", e => e.ConfigureConsumer<ShipmentDeliveredConsumer>(ctx));
            cfg.ReceiveEndpoint("notification-shipment-cancelled", e => e.ConfigureConsumer<ShipmentCancelledConsumer>(ctx));
            cfg.ReceiveEndpoint("notification-payment-completed", e => e.ConfigureConsumer<PaymentCompletedConsumer>(ctx));
            cfg.ReceiveEndpoint("notification-payment-refunded", e => e.ConfigureConsumer<PaymentRefundedConsumer>(ctx));
            cfg.ReceiveEndpoint("notification-payment-failed", e => e.ConfigureConsumer<PaymentFailedConsumer>(ctx));

        });
    });

    var jwt = builder.Configuration.GetSection("JwtSettings");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt => opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
        });

    builder.Services.AddAuthorization();

    builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IEmailService, EmailService>();

    builder.Services.AddCors(opt =>  opt.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

    builder.Services.AddSingleton<IConnection>(sp =>
    {
        var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var factory = new ConnectionFactory
        {
            Uri = new Uri($"amqp://guest:guest@{host}:5672"),
            AutomaticRecoveryEnabled = true
        };
        return factory.CreateConnectionAsync().GetAwaiter().GetResult();
    });

    builder.Services.AddHealthChecks()
        .AddSqlServer(
            connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
            name: "sqlserver",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "db" })
        .AddRabbitMQ(
            name: "rabbitmq",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "messaging" });

    var app = builder.Build();

    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        AllowCachingResponses = false,
        ResultStatusCodes =
    {
        [HealthStatus.Healthy]   = StatusCodes.Status200OK,
        [HealthStatus.Degraded]  = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    },
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = new
            {
                service = "NotificationService",
                status = report.Status.ToString(),
                timestamp = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt"),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description ?? (e.Value.Status == HealthStatus.Healthy ? "OK" : "Check failed"),
                    durationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 2)
                })
            };
            await context.Response.WriteAsync(
                System.Text.Json.JsonSerializer.Serialize(result,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
    });

    app.UseMiddleware<ExceptionMiddleware>();
    app.UseSerilogRequestLogging(opt =>
        opt.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0000}ms");

    if (!app.Environment.IsEnvironment("Testing"))
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<NotificationDbContext>().Database.Migrate();
    }

    app.UseSwagger(); app.UseSwaggerUI();
    app.UseCors("AllowAll");
    app.UseAuthentication(); app.UseAuthorization();
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, " !! NotificationService crashed on startup.");
}
finally
{
    Log.CloseAndFlush();
}