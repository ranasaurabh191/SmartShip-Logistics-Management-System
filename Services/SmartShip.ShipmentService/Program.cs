using FluentValidation;
using FluentValidation.AspNetCore;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SmartShip.ShipmentService.Data;
using SmartShip.ShipmentService.Messaging.Consumers;
using SmartShip.ShipmentService.Middleware;
using SmartShip.ShipmentService.Services;
using SmartShip.ShipmentService.Validators;
using System.Text;
using System.Text.Json.Serialization;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information(" --> Starting ShipmentService...");

    var builder = WebApplication.CreateBuilder(args);

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

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<UserDeletedConsumer>();
        x.AddConsumer<PaymentCompletedConsumer>();

        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host("localhost", "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });

            cfg.ReceiveEndpoint("shipment-user-deleted", e =>
            {
                e.ConfigureConsumer<UserDeletedConsumer>(ctx);
            });
            cfg.ReceiveEndpoint("shipment-payment-completed", e =>
            {
                e.ConfigureConsumer<PaymentCompletedConsumer>(ctx);
            });
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
    builder.Services.AddScoped<IShipmentService, ShipmentService>();
    builder.Services.AddScoped<UserDeletedConsumer>();
    builder.Services.AddCors(opt =>
        opt.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

    var app = builder.Build();
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseSerilogRequestLogging(opt =>
        opt.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0000}ms");

    using (var scope = app.Services.CreateScope())
        scope.ServiceProvider.GetRequiredService<ShipmentDbContext>().Database.Migrate();

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