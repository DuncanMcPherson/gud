using gud.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace gud.Server.Data;

public class GudDbContext(DbContextOptions<GudDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Repo> Repos => Set<Repo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<Repo>()
            .HasIndex(r => new { r.OwnerId, r.Name })
            .IsUnique();

        modelBuilder.Entity<Repo>()
            .HasOne(r => r.Owner)
            .WithMany(u => u.Repos)
            .HasForeignKey(r => r.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}