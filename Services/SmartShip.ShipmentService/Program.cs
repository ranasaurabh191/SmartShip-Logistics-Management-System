using FluentValidation;
using FluentValidation.AspNetCore;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SmartShip.ShipmentService.API.Middleware;
using SmartShip.ShipmentService.Core.Interfaces.Persistence;
using SmartShip.ShipmentService.Core.Interfaces.Repositories;
using SmartShip.ShipmentService.Core.Interfaces.Services;
using SmartShip.ShipmentService.Core.Sagas;
using SmartShip.ShipmentService.Core.Services;
using SmartShip.ShipmentService.Core.Validators;
using SmartShip.ShipmentService.Domain.Entities;
using SmartShip.ShipmentService.Infrastructure.Data;
using SmartShip.ShipmentService.Infrastructure.Messaging.Consumers;
using SmartShip.ShipmentService.Infrastructure.Persistence;
using SmartShip.ShipmentService.Infrastructure.Repositories;
using System.Text;
using System.Text.Json.Serialization;
using RabbitMQ.Client;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information(" --> Starting ShipmentService...");

    var builder = WebApplication.CreateBuilder(args);
    var isTesting = builder.Environment.IsEnvironment("Testing");
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "ShipmentService")
        .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName));

    builder.Services.AddControllers()
        .AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.Converters
                .Add(new JsonStringEnumConverter());
        })
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

    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddFluentValidationClientsideAdapters();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddValidatorsFromAssemblyContaining<CreateShipmentRequestValidator>();

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

    builder.Services.AddDbContext<ShipmentDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddHttpClient("PaymentService", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:PaymentService"]!);
    });

    builder.Services.AddHttpClient("IdentityService", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:IdentityService"]!);
    });

    builder.Services.AddHttpContextAccessor();

    var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";

    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<UserDeletedConsumer>();
        x.AddConsumer<CancelShipmentConsumer>();

        if (isTesting)
        {
            x.AddSagaStateMachine<ShipmentOrderStateMachine, ShipmentOrderState>()
                .InMemoryRepository();

            x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
        }
        else
        {
            x.AddSagaStateMachine<ShipmentOrderStateMachine, ShipmentOrderState>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                    r.ExistingDbContext<ShipmentDbContext>();
                    r.UseSqlServer();
                });

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(rabbitHost, "/", h =>
                {
                    h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
                    h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
                });
                cfg.ReceiveEndpoint("shipment-user-deleted", e =>
                    e.ConfigureConsumer<UserDeletedConsumer>(ctx));
                cfg.ReceiveEndpoint("shipment-order-state", e =>
                    e.ConfigureSaga<ShipmentOrderState>(ctx));
                cfg.ReceiveEndpoint("shipment-cancel-command", e =>
                    e.ConfigureConsumer<CancelShipmentConsumer>(ctx));
            });
        }
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
    builder.Services.AddScoped<IShipmentRepository, ShipmentRepository>();
    builder.Services.AddScoped<IAddressRepository, AddressRepository>();
    builder.Services.AddScoped<IPackageRepository, PackageRepository>();
    builder.Services.AddScoped<IShipmentOrderSagaRepository, ShipmentOrderSagaRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IShipmentService, ShipmentService>();

    builder.Services.AddCors(opt => opt.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
    if (!isTesting)
    {
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
    }

    var healthBuilder = builder.Services.AddHealthChecks();
    if (!isTesting)
    {
        healthBuilder
            .AddSqlServer(
                connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
                name: "sqlserver",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db" })
            .AddRabbitMQ(
                name: "rabbitmq",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "messaging" });
    }

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
                service = "ShipmentService",
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

    if (!app.Environment.IsEnvironment("Testing"))
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ShipmentDbContext>().Database.Migrate();
    }

    app.UseSwagger(); app.UseSwaggerUI();
    app.UseCors("AllowAll");
    app.UseAuthentication(); app.UseAuthorization();
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, " !! ShipmentService crashed on startup.");
}
finally
{
    Log.CloseAndFlush();
}