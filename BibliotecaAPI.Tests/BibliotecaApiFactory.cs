using BibliotecaAPI.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BibliotecaAPI.Tests;

/// <summary>
/// Boots the real API with the Npgsql provider swapped for an isolated EF InMemory
/// database, so endpoint behavior is exercised without a live PostgreSQL instance.
/// A new instance uses a fresh database (xUnit creates one test-class instance per test).
/// </summary>
public class BibliotecaApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Drop every EF registration tied to the app's provider (Npgsql), including the
            // EF 9 IDbContextOptionsConfiguration<> carriers — otherwise re-registering a second
            // provider throws "Only a single database provider can be registered".
            var efDescriptors = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<BibliotecaContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration"))
                .ToList();
            foreach (var descriptor in efDescriptors)
                services.Remove(descriptor);

            services.AddDbContext<BibliotecaContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
