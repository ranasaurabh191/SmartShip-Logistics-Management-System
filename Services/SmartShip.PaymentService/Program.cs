using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SmartShip.PaymentService.API.Middleware;
using SmartShip.PaymentService.Core.Interfaces.Services;
using SmartShip.PaymentService.Core.Services;
using SmartShip.PaymentService.Infrastructure.Data;
using SmartShip.PaymentService.Infrastructure.Messaging.Consumers;
using SmartShip.PaymentService.Core.Interfaces.Persistence;
using SmartShip.PaymentService.Core.Interfaces.Repositories;
using SmartShip.PaymentService.Infrastructure.Persistence;
using SmartShip.PaymentService.Infrastructure.Repositories;
using System.Text;
using RabbitMQ.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information(" --> Starting PaymentService...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "PaymentService")
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

                return new BadRequestObjectResult(new
                {
                    message = "Validation failed.",
                    errors
                });
            };
        });

    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Payment Service",
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
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddHttpClient("ShipmentService", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:ShipmentService"]!);
    });
    builder.Services.AddDbContext<PaymentDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
    builder.Services.AddScoped<ISagaCorrelationRepository, SagaCorrelationRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IPaymentService, PaymentService>();

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var jwt = builder.Configuration.GetSection("JwtSettings");

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt["Issuer"],
                ValidAudience = jwt["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwt["Key"]!))
            };
        });

    builder.Services.AddAuthorization();

    var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";

    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<ShipmentCreatedConsumer>();
        x.AddConsumer<ShipmentCancelledByCustomerConsumer>();
        x.AddConsumer<UserDeletedConsumer>();

        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(rabbitHost, "/", h =>
            {
                h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
                h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
            });
            cfg.ReceiveEndpoint("payment-shipment-created", e =>
            {
                e.ConfigureConsumer<ShipmentCreatedConsumer>(context);
            });
            cfg.ReceiveEndpoint("payment-shipment-cancelled-by-customer", e =>
            {
                e.ConfigureConsumer<ShipmentCancelledByCustomerConsumer>(context);
            });
            cfg.ReceiveEndpoint("payment-user-deleted", e =>
            {
                e.ConfigureConsumer<UserDeletedConsumer>(context);
            });
        });
    });

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

    app.UseMiddleware<ExceptionMiddleware>();

    app.UseSerilogRequestLogging(opt =>
        opt.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0000}ms");
    if (!app.Environment.IsEnvironment("Testing"))
    {
        {
            var retries = 5;
            while (retries > 0)
            {
                try
                {
                    using var scope = app.Services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
                    db.Database.Migrate();
                    Log.Information("Payment database migrated successfully.");
                    break;
                }
                catch (Exception ex)
                {
                    retries--;
                    Log.Warning("Migration failed ({Retries} left): {Message}", retries, ex.Message);
                    Thread.Sleep(5000);
                }
            }
        }
    }

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors("AllowAll");
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
                service = "PaymentService",
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
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, " !! PaymentService crashed on startup.");
}
finally
{
    Log.CloseAndFlush();
}
public partial class Program { }