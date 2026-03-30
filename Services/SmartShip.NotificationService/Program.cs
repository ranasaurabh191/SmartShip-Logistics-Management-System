using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SmartShip.NotificationService.Messaging.Consumers;
using SmartShip.NotificationService.Data;
using SmartShip.NotificationService.Middleware;
using SmartShip.NotificationService.Services;
using System.Text;
using SmartShip.NotificationService.Consumers;

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

    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<UserCreatedConsumer>();
        x.AddConsumer<ShipmentCreatedConsumer>();
        x.AddConsumer<ShipmentStatusUpdatedConsumer>();
        x.AddConsumer<ShipmentDeliveredConsumer>();
        x.AddConsumer<ShipmentCancelledConsumer>();
        x.AddConsumer<PaymentCompletedConsumer>();

        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host("localhost", "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });

            cfg.ReceiveEndpoint("notification-user-created", e =>
                e.ConfigureConsumer<UserCreatedConsumer>(ctx));
            cfg.ReceiveEndpoint("notification-shipment-created", e =>
                e.ConfigureConsumer<ShipmentCreatedConsumer>(ctx));
            cfg.ReceiveEndpoint("notification-status-updated", e =>
                e.ConfigureConsumer<ShipmentStatusUpdatedConsumer>(ctx));
            cfg.ReceiveEndpoint("notification-shipment-delivered", e =>
                e.ConfigureConsumer<ShipmentDeliveredConsumer>(ctx));
            cfg.ReceiveEndpoint("notification-shipment-cancelled", e =>
                e.ConfigureConsumer<ShipmentCancelledConsumer>(ctx));
            cfg.ReceiveEndpoint("notification-payment-completed", e =>
                e.ConfigureConsumer<PaymentCompletedConsumer>(ctx));
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
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddCors(opt =>
        opt.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

    var app = builder.Build();
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseSerilogRequestLogging(opt =>
        opt.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0000}ms");

    if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
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