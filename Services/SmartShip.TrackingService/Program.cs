using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using Serilog;
using SmartShip.TrackingService.API.Middleware;
using SmartShip.TrackingService.Core.Interfaces.Persistence;
using SmartShip.TrackingService.Core.Interfaces.Repositories;
using SmartShip.TrackingService.Core.Interfaces.Services;
using SmartShip.TrackingService.Core.Services;
using SmartShip.TrackingService.Infrastructure.Data;
using SmartShip.TrackingService.Infrastructure.Messaging.Consumers;
using SmartShip.TrackingService.Infrastructure.Persistence;
using SmartShip.TrackingService.Infrastructure.Repositories;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information(" --> Starting TrackingService...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "TrackingService")
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
            Title = "Shipment Service",
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

    builder.Services.AddDbContext<TrackingDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<ShipmentCreatedConsumer>();
        x.AddConsumer<ShipmentStatusUpdatedConsumer>();
        x.AddConsumer<PaymentCreatedConsumer>();
        x.AddConsumer<PaymentCompletedTrackingConsumer>();
        x.AddConsumer<PaymentFailedTrackingConsumer>();
        x.AddConsumer<PaymentRefundedTrackingConsumer>();
        x.AddConsumer<ShipmentDeliveredConsumer>();

        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host("localhost", "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });
            cfg.ReceiveEndpoint("tracking-shipment-created", e =>
                e.ConfigureConsumer<ShipmentCreatedConsumer>(ctx));
            cfg.ReceiveEndpoint("tracking-status-updated", e =>
                e.ConfigureConsumer<ShipmentStatusUpdatedConsumer>(ctx));
            cfg.ReceiveEndpoint("tracking-payment-created", e =>
                e.ConfigureConsumer<PaymentCreatedConsumer>(ctx));
            cfg.ReceiveEndpoint("tracking-payment-completed", e =>
                e.ConfigureConsumer<PaymentCompletedTrackingConsumer>(ctx));
            cfg.ReceiveEndpoint("tracking-failed-payment", e =>
                e.ConfigureConsumer<PaymentFailedTrackingConsumer>(ctx));
            cfg.ReceiveEndpoint("tracking-payment-refunded", e =>
                e.ConfigureConsumer<PaymentRefundedTrackingConsumer>(ctx));
            cfg.ReceiveEndpoint("tracking-shipment-delivered", e =>
                e.ConfigureConsumer<ShipmentDeliveredConsumer>(ctx));
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

    builder.Services.AddScoped<ITrackingEventRepository, TrackingEventRepository>();
    builder.Services.AddScoped<IDeliveryProofRepository, DeliveryProofRepository>();
    builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<ITrackingService, TrackingService>();

    builder.Services.AddCors(opt => opt.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

    builder.Services.AddSingleton<IConnection>(sp =>
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri("amqp://guest:guest@localhost:5672/"),
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
                service = "TrackingService",
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
    app.UseSerilogRequestLogging(opt => opt.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0000}ms");

    using (var scope = app.Services.CreateScope()) scope.ServiceProvider.GetRequiredService<TrackingDbContext>().Database.Migrate();

    app.UseSwagger(); app.UseSwaggerUI();
    app.UseCors("AllowAll");
    app.UseAuthentication(); 
    app.UseAuthorization();
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, " !! TrackingService crashed on startup.");
}
finally
{
    Log.CloseAndFlush();
}