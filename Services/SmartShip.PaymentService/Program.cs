using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SmartShip.PaymentService.Data;
using SmartShip.PaymentService.Middleware;
using SmartShip.PaymentService.Services;
using System.Text;

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

    builder.Services.AddHttpClient("ShipmentService", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:ShipmentService"]!);
    });

    builder.Services.AddDbContext<PaymentDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

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

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddAuthorization();

    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<SmartShip.PaymentService.Messaging.Consumers.ShipmentCreatedConsumer>();

        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host("localhost", "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });

            cfg.ReceiveEndpoint("payment-shipment-created", e =>
            {
                e.ConfigureConsumer<SmartShip.PaymentService.Messaging.Consumers.ShipmentCreatedConsumer>(context);
            });
        });
    });

    builder.Services.AddScoped<SmartShip.PaymentService.Messaging.Consumers.ShipmentCreatedConsumer>();

    builder.Services.AddCors(opt =>
        opt.AddPolicy("AllowAll", p =>
            p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

    var app = builder.Build();

    app.UseMiddleware<ExceptionMiddleware>();

    app.UseSerilogRequestLogging(opt =>
        opt.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0000}ms");

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        db.Database.Migrate();
    }

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors("AllowAll");

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