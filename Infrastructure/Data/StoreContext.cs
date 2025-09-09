using System;
using Core.Entities.AMFI;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class StoreContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SchemeDetail>()
            .Property(a => a.Nav)
            .HasPrecision(18, 6);
    }

    public DbSet<ApprovedData> ApprovedData { get; set; }
    public DbSet<SchemeDetail> SchemeDetails { get; set; }
}
