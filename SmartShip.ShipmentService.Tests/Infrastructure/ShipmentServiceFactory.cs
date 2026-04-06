using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SmartShip.ShipmentService.Infrastructure.Data;
using System.Text;

namespace SmartShip.ShipmentService.Tests.Infrastructure;

public class ShipmentServiceFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName;

    public ShipmentServiceFactory()
        : this("ShipmentTestDb_" + Guid.NewGuid()) { }

    public ShipmentServiceFactory(string dbName)
    {
        _dbName = dbName;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing"); 

        builder.ConfigureServices(services =>
        {
            var efDescriptors = services
        .Where(d =>
            d.ServiceType?.Namespace != null &&
            (d.ServiceType.Namespace.StartsWith("Microsoft.EntityFrameworkCore") ||
             d.ImplementationType?.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true ||
             d.ServiceType == typeof(ShipmentDbContext) ||
             d.ServiceType == typeof(DbContextOptions<ShipmentDbContext>) ||
             d.ServiceType == typeof(DbContextOptions)))
        .ToList();

            foreach (var d in efDescriptors)
                services.Remove(d);

            services.AddDbContext<ShipmentDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName)
                       .ConfigureWarnings(w =>
                           w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = TestJwtHelper.Issuer,
                        ValidateAudience = true,
                        ValidAudience = TestJwtHelper.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(TestJwtHelper.SecretKey)),
                        ValidateLifetime = false 
                    };
                });
        });
    }
}