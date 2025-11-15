using Core.Entities.Investment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Config
{
    public class InvestmentConfiguration : IEntityTypeConfiguration<Investment>
    {
        public void Configure(EntityTypeBuilder<Investment> builder)
        {
            builder.ToTable("Investments");

            builder.HasKey(i => i.Id);

            // Configure properties
            builder.Property(i => i.SchemeCode)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(i => i.SchemeName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(i => i.FundName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(i => i.InvestAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(i => i.NumberOfUnits)
                .IsRequired()
                .HasColumnType("decimal(18,4)");

            builder.Property(i => i.ModeOfInvestment)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(i => i.ImagePath)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(i => i.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("in progress");

            builder.Property(i => i.IsPublished)
                .HasDefaultValue(false);

            builder.Property(i => i.InvestBy)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(i => i.IsApproved)
                .HasDefaultValue(false);

            builder.Property(i => i.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(i => i.Remarks)
                .HasMaxLength(1000)
                .IsRequired(false);

            // Configure relationships with NO ACTION (RESTRICT)
            builder.HasOne(i => i.Investor)
                .WithMany()
                .HasForeignKey(i => i.InvestorId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(i => i.CreatedByUser)
                .WithMany()
                .HasForeignKey(i => i.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // Create indexes
            builder.HasIndex(i => i.InvestorId);
            builder.HasIndex(i => i.SchemeCode);
            builder.HasIndex(i => i.Status);
            builder.HasIndex(i => i.IsApproved);
            builder.HasIndex(i => i.CreatedAt);
        }
    }
}