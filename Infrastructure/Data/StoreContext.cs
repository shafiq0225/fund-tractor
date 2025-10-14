using System;
using Core.Entities.AMFI;
using Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class StoreContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SchemeDetail>()
            .Property(a => a.Nav)
            .HasPrecision(18, 6);

        // UserRole configuration
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasOne(ur => ur.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(ur => ur.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public DbSet<ApprovedData> ApprovedData { get; set; }
    public DbSet<SchemeDetail> SchemeDetails { get; set; }

    // New DbSets for Users
    public DbSet<User> Users { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

}
