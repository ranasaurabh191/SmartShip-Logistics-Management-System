using FluentValidation;
using FluentValidation.AspNetCore;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SmartShip.IdentityService.API.Middleware;
using SmartShip.IdentityService.Core.DTOs;
using SmartShip.IdentityService.Core.Interfaces.Persistence;
using SmartShip.IdentityService.Core.Interfaces.Repositories;
using SmartShip.IdentityService.Core.Interfaces.Services;
using SmartShip.IdentityService.Core.Services;
using SmartShip.IdentityService.Core.Validators;
using SmartShip.IdentityService.Infrastructure.Data;
using SmartShip.IdentityService.Infrastructure.Persistence;
using SmartShip.IdentityService.Infrastructure.Repositories;
using SmartShip.NotificationService.Core.Interfaces.Services;
using SmartShip.NotificationService.Infrastructure.Services;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information(" --> Starting IdentityService...");

    var builder = WebApplication.CreateBuilder(args);
    var internalKey = builder.Configuration["InternalApi:ApiKey"];

    var urls = builder.Configuration.GetSection("ServiceUrls");
    builder.Services.AddHttpClient("IdentityService", c =>
    {
        c.BaseAddress = new Uri(urls["IdentityService"]!);
        c.DefaultRequestHeaders.Add("X-Internal-Key", internalKey);  
    });

    builder.Services.AddScoped<IValidator<SignupOtpRequest>, SignupOtpRequestValidator>();
    builder.Services.AddScoped<IValidator<VerifyOtpRequest>, VerifyOtpRequestValidator>();

    builder.Services.AddHttpClient("ShipmentService", c =>
    {
        c.BaseAddress = new Uri(urls["ShipmentService"]!);
        c.DefaultRequestHeaders.Add("X-Internal-Key", internalKey);
    });
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "IdentityService")
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

    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddFluentValidationClientsideAdapters();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddValidatorsFromAssemblyContaining<SignupRequestValidator>();

    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Identity Service",
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

    builder.Services.AddDbContext<IdentityDbContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddMassTransit(x =>
    {
        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host("localhost", "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
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

    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IOtpVerificationRepository, OtpVerificationRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUserService, UserService>();

    builder.Services.AddCors(opt =>
        opt.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

    var app = builder.Build();
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseSerilogRequestLogging(opt =>
        opt.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0000}ms");

    using (var scope = app.Services.CreateScope())
        scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.Migrate();

    app.UseSwagger(); app.UseSwaggerUI();
    app.UseCors("AllowAll");
    app.UseAuthentication(); app.UseAuthorization();
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, " !! IdentityService crashed on startup.");
}
finally
{
    Log.CloseAndFlush();
}