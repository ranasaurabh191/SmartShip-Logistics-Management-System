using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("🚀 Starting SmartShip Gateway...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Gateway")
        .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName));

    var jwtKey = "SmartShip$SuperSecret$Key$2026!@#XYZ";
    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", opt =>
        {
            opt.RequireHttpsMetadata = false;
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "SmartShipGateway",
                ValidAudience = "SmartShipClients",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });

    builder.Services.AddCors(opt =>
        opt.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
    builder.Services.AddOcelot(builder.Configuration);

    var app = builder.Build();

    app.UseSerilogRequestLogging(opt =>
        opt.MessageTemplate = "GATEWAY {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0000}ms");

    app.UseCors("AllowAll");
    app.UseAuthentication();

    app.MapGet("/", () => "SmartShip Gateway v1.0 Running");
    app.MapGet("/health", () => Results.Json(new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow,
        services = new[] { "identity:5001", "shipment:5002", "tracking:5003", "admin:5004" }
    }));

    app.UseWhen(
        ctx => ctx.Request.Path.StartsWithSegments("/gateway"),
        ocelotBranch => ocelotBranch.UseOcelot().Wait()
    );

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Gateway crashed on startup.");
}
finally
{
    Log.CloseAndFlush();
}