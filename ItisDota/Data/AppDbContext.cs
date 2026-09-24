using Microsoft.EntityFrameworkCore;
using ItisDota.Data.Entities;

namespace ItisDota.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<PlayerGroup> PlayerGroups => Set<PlayerGroup>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasIndex(p => p.KeycloakUserId).IsUnique();
            entity.HasIndex(p => p.TgTag).IsUnique();
            entity.Property(p => p.KeycloakUserId).HasMaxLength(100).IsRequired();
            entity.Property(p => p.RealName).HasMaxLength(200).IsRequired();
            entity.Property(p => p.TgTag).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<PlayerGroup>(entity =>
        {
            entity.HasIndex(g => g.InviteToken).IsUnique();
            entity.Property(g => g.Name).HasMaxLength(100).IsRequired();
            entity.Property(g => g.InviteToken).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasIndex(m => new { m.GroupId, m.PlayerId }).IsUnique();
            entity.Property(m => m.SelectionCount).HasDefaultValue(0);
            entity.HasOne(m => m.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(m => m.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(m => m.Player)
                .WithMany(p => p.Memberships)
                .HasForeignKey(m => m.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
