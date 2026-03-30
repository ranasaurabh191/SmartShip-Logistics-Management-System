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
    Log.Information(" --> Starting SmartShip Gateway...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Gateway")
        .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName));

    var jwt = builder.Configuration.GetSection("JwtSettings");
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
                ValidIssuer = jwt["Issuer"],
                ValidAudience = jwt["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
            };
        });

    builder.Services.AddCors(opt => opt.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
    
    builder.Services.AddOcelot(builder.Configuration);
    builder.Services.AddSwaggerForOcelot(builder.Configuration);

    var app = builder.Build();

    app.UseSerilogRequestLogging(opt =>
        opt.MessageTemplate = "GATEWAY {RequestMethod} {RequestPath} -> {StatusCode} in {Elapsed:0.0000}ms");

    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/", () => "SmartShip Gateway Running");
    app.MapGet("/health", () => Results.Json(new
    {
        status = "healthy",
        timestamp = DateTime.Now,
        services = new[] { "identity:5001", "shipment:5002", "tracking:5003", "admin:5004", "payment:5005" }
    }));

    app.UseSwaggerForOcelotUI(opt =>
    {
        opt.PathToSwaggerGenerator = "/swagger/docs";
    },
    uiOpt =>   
    {
        uiOpt.OAuthClientId("swagger-ui");
        uiOpt.OAuthAppName("SmartShip Swagger UI");
        uiOpt.OAuthUsePkce();
        uiOpt.ConfigObject.AdditionalItems["persistAuthorization"] = true;
    });
    
    await app.UseOcelot();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, " !! Gateway crashed on startup.");
}
finally
{
    Log.CloseAndFlush();
}