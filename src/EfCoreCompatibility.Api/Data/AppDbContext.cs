using EfCoreCompatibility.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreCompatibility.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbContextOptions<AppDbContext> ProviderOptions { get; } = options;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
