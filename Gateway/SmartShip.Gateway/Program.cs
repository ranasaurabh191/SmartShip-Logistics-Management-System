using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

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

app.UseCors("AllowAll");
app.UseAuthentication();


app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/gateway"),
    ocelotBranch =>
    {
        ocelotBranch.UseOcelot().Wait();
    }
);

app.MapGet("/", () => "SmartShip Gateway v1.0 Running");
app.MapGet("/health", () => Results.Json(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    services = new[] { "identity:5001", "shipment:5002", "tracking:5003", "admin:5004" }
}));

app.Run();