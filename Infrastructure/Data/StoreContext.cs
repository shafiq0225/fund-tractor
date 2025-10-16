using System;
using Core.Entities.AMFI;
using Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class StoreContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Your existing AMFI configuration
        modelBuilder.Entity<SchemeDetail>()
            .Property(a => a.Nav)
            .HasPrecision(18, 6);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.PanNumber).IsUnique();
            entity.HasIndex(u => u.EmployeeId).IsUnique().HasFilter("[EmployeeId] IS NOT NULL");

            entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(u => u.IsActive).HasDefaultValue(true);

            // Family relationship
            entity.HasOne(u => u.FamilyHead)
                  .WithMany(u => u.FamilyMembers)
                  .HasForeignKey(u => u.FamilyHeadId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // UserRole configuration
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasOne(ur => ur.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(ur => ur.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // UserProfile configuration
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasOne(up => up.User)
                  .WithOne(u => u.UserProfile)
                  .HasForeignKey<UserProfile>(up => up.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // RolePermission configuration
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasIndex(rp => new { rp.RoleName, rp.PermissionId }).IsUnique();
        });

        // Seed initial roles and permissions
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed permissions
        var permissions = new List<Permission>
    {
        new Permission { Id = 1, Name = "ViewDashboard", Description = "Access to dashboard" },
        new Permission { Id = 2, Name = "ManageUsers", Description = "Create, edit, delete users" },
        new Permission { Id = 3, Name = "ViewReports", Description = "Access to all reports" },
        new Permission { Id = 4, Name = "ManageInvestments", Description = "Manage investment portfolios" },
        new Permission { Id = 5, Name = "ViewFamilyData", Description = "View family member data" },
        new Permission { Id = 6, Name = "ManageFamily", Description = "Add/remove family members" },
        new Permission { Id = 7, Name = "ExportData", Description = "Export data to Excel" },
        new Permission { Id = 8, Name = "SystemConfig", Description = "Configure system settings" }
    };
        modelBuilder.Entity<Permission>().HasData(permissions);

        // Seed role permissions
        var rolePermissions = new List<RolePermission>
    {
        // Admin - Full access
        new RolePermission { Id = 1, RoleName = "Admin", PermissionId = 1 },
        new RolePermission { Id = 2, RoleName = "Admin", PermissionId = 2 },
        new RolePermission { Id = 3, RoleName = "Admin", PermissionId = 3 },
        new RolePermission { Id = 4, RoleName = "Admin", PermissionId = 4 },
        new RolePermission { Id = 5, RoleName = "Admin", PermissionId = 5 },
        new RolePermission { Id = 6, RoleName = "Admin", PermissionId = 6 },
        new RolePermission { Id = 7, RoleName = "Admin", PermissionId = 7 },
        new RolePermission { Id = 8, RoleName = "Admin", PermissionId = 8 },

        // Employee - Limited access
        new RolePermission { Id = 9, RoleName = "Employee", PermissionId = 1 },
        new RolePermission { Id = 10, RoleName = "Employee", PermissionId = 3 },
        new RolePermission { Id = 11, RoleName = "Employee", PermissionId = 4 },
        new RolePermission { Id = 12, RoleName = "Employee", PermissionId = 7 },

        // HeadOfFamily - Family management + personal access
        new RolePermission { Id = 13, RoleName = "HeadOfFamily", PermissionId = 1 },
        new RolePermission { Id = 14, RoleName = "HeadOfFamily", PermissionId = 4 },
        new RolePermission { Id = 15, RoleName = "HeadOfFamily", PermissionId = 5 },
        new RolePermission { Id = 16, RoleName = "HeadOfFamily", PermissionId = 6 },
        new RolePermission { Id = 17, RoleName = "HeadOfFamily", PermissionId = 7 },

        // FamilyMember - Personal access only
        new RolePermission { Id = 18, RoleName = "FamilyMember", PermissionId = 1 },
        new RolePermission { Id = 19, RoleName = "FamilyMember", PermissionId = 4 }
    };
        modelBuilder.Entity<RolePermission>().HasData(rolePermissions);

    }

    public DbSet<ApprovedData> ApprovedData { get; set; }
    public DbSet<SchemeDetail> SchemeDetails { get; set; }

    // New DbSets for Users and Permissions
    public DbSet<User> Users { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

}
