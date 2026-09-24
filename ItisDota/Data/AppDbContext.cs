using Microsoft.EntityFrameworkCore;
using WebApplication1.Data.Entities;

namespace WebApplication1.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasIndex(p => p.TgTag).IsUnique();
            entity.Property(p => p.RealName).HasMaxLength(200).IsRequired();
            entity.Property(p => p.TgTag).HasMaxLength(100).IsRequired();
            entity.Property(p => p.MmRRange).HasMaxLength(50).IsRequired();
            entity.Property(p => p.SelectionCount).HasDefaultValue(0);
        });

        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasIndex(s => s.Key).IsUnique();
            entity.Property(s => s.Key).HasMaxLength(100).IsRequired();
            entity.Property(s => s.Value).IsRequired();
        });
    }
}
