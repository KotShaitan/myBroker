using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<Item> Items => Set<Item>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseNpgsql("Host=localhost;Port=55432;Database=cache_comparison;Username=user;Password=password");
}
