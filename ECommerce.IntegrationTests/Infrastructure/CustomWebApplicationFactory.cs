using DAL.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ECommerce.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // One database name is used by this factory instance.
    // Therefore, test data inserted by a test is visible to API requests.
    private readonly string _databaseName =
        $"ECommerceIntegrationTestDb-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove SQL Server configuration from API Program.cs.
            services.RemoveAll<DbContextOptions<ECommerceDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ECommerceDbContext>>();
            services.RemoveAll<ECommerceDbContext>();

            // Add EF Core InMemory database for integration tests.
            services.AddDbContext<ECommerceDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });
        });
    }
}