using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using SmartShip.Gateway.HealthChecks;
using System.Text;
using System.Text.Json;

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

    builder.Services.AddCors(opt =>
        opt.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

    builder.Services.AddHttpClient("HealthCheckClient", c =>
    {
        c.Timeout = TimeSpan.FromSeconds(3);
    });

    builder.Services.AddHealthChecks()
        .AddCheck<DownstreamServicesHealthCheck>(
            "downstream_services",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "services" });


    builder.Services.AddOcelot(builder.Configuration);
    builder.Services.AddSwaggerForOcelot(builder.Configuration);

    var app = builder.Build();

    app.UseSerilogRequestLogging(opt =>
        opt.MessageTemplate = "GATEWAY {RequestMethod} {RequestPath} -> {StatusCode} in {Elapsed:0.0000}ms");

    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/", () => " --> SmartShip Gateway Running");

    app.MapHealthChecks("/health", new HealthCheckOptions
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

            var serviceData = report.Entries
                .SelectMany(e => e.Value.Data)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var response = new
            {
                gateway = "SmartShip Gateway",
                status = report.Status.ToString(),
                timestamp = DateTime.Now.ToString("dd-MMM-yyyy hh:mm:ss tt"),
                totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2) + " ms",
                services = serviceData,
                summary = new
                {
                    total = serviceData.Count,
                    healthy = serviceData.Values.Count(v => v?.ToString()?.Contains("Healthy") == true),
                    unhealthy = serviceData.Values.Count(v => v?.ToString()?.Contains("Unreachable") == true
                                                           || v?.ToString()?.Contains("Timeout") == true),
                    degraded = serviceData.Values.Count(v => v?.ToString()?.Contains("Degraded") == true)
                }
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
        }
    });

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

    app.UseWhen(
        ctx => ctx.Request.Path.StartsWithSegments("/gateway"),
        ocelotBranch => ocelotBranch.UseOcelot().Wait()
    );

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