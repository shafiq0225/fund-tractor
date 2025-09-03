using System;
using Core.Entities.AMFI;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class StoreContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //modelBuilder.Entity<Fund>().HasKey(f => f.FundId);

        //modelBuilder.Entity<Scheme>().HasKey(s => s.SchemeId);
        //modelBuilder.Entity<Scheme>()
        //    .HasOne(s => s.Fund)
        //    .WithMany(f => f.Schemes)
        //    .HasForeignKey(s => s.FundId);

        modelBuilder.Entity<SchemeDetail>()
            .Property(a => a.Nav)
            .HasPrecision(18, 6);
    }


    public DbSet<ApprovedData> ApprovedData { get; set; }
    public DbSet<SchemeDetail> SchemeDetails { get; set; }
}
