using BuildingFex.Api.Finances.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace BuildingFex.Api.Finances.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyFinancesConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Fee>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Id).ValueGeneratedOnAdd();
            entity.Property(f => f.ExternalId).HasMaxLength(64).IsRequired();
            entity.HasIndex(f => f.ExternalId).IsUnique();
            entity.Property(f => f.ResidentExternalId).HasMaxLength(64).IsRequired();
            entity.Property(f => f.Month).HasMaxLength(40).IsRequired();
            entity.Property(f => f.DueDate).HasMaxLength(20).IsRequired();
            entity.Property(f => f.Status).HasMaxLength(20).IsRequired();
            entity.Property(f => f.Amount).HasPrecision(18, 2);
            entity.HasOne(f => f.OwnerAdmin).WithMany().HasForeignKey(f => f.OwnerAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).ValueGeneratedOnAdd();
            entity.Property(p => p.ExternalId).HasMaxLength(64).IsRequired();
            entity.HasIndex(p => p.ExternalId).IsUnique();
            entity.Property(p => p.ResidentExternalId).HasMaxLength(64).IsRequired();
            entity.Property(p => p.FeeExternalId).HasMaxLength(64);
            entity.Property(p => p.FeeMonth).HasMaxLength(40);
            entity.Property(p => p.PaidAt).HasMaxLength(40);
            entity.Property(p => p.Method).HasMaxLength(80);
            entity.Property(p => p.Reference).HasMaxLength(80);
            entity.Property(p => p.Amount).HasPrecision(18, 2);
            entity.HasOne(p => p.OwnerAdmin).WithMany().HasForeignKey(p => p.OwnerAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Receipt>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).ValueGeneratedOnAdd();
            entity.Property(r => r.ExternalId).HasMaxLength(64).IsRequired();
            entity.HasIndex(r => r.ExternalId).IsUnique();
            entity.Property(r => r.ResidentExternalId).HasMaxLength(64).IsRequired();
            entity.Property(r => r.IssueDate).HasMaxLength(20);
            entity.Property(r => r.DueDate).HasMaxLength(20);
            entity.Property(r => r.Status).HasMaxLength(20).IsRequired();
            entity.Property(r => r.Concept).HasMaxLength(200);
            entity.Property(r => r.Amount).HasPrecision(18, 2);
            entity.Property(r => r.LateFee).HasPrecision(18, 2);
            entity.Property(r => r.ExtraCharges).HasPrecision(18, 2);
            entity.HasOne(r => r.OwnerAdmin).WithMany().HasForeignKey(r => r.OwnerAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FinanceSetting>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).ValueGeneratedOnAdd();
            entity.Property(s => s.ExternalId).HasMaxLength(64).IsRequired();
            entity.HasIndex(s => s.ExternalId).IsUnique();
            entity.Property(s => s.BaseMonthlyExpense).HasPrecision(18, 2);
            entity.Property(s => s.LateFeeRate).HasPrecision(8, 4);
            entity.HasOne(s => s.OwnerAdmin).WithMany().HasForeignKey(s => s.OwnerAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<KpiRecord>(entity =>
        {
            entity.HasKey(k => k.Id);
            entity.Property(k => k.Id).ValueGeneratedOnAdd();
            entity.Property(k => k.ExternalId).HasMaxLength(64).IsRequired();
            entity.HasIndex(k => k.ExternalId).IsUnique();
            entity.Property(k => k.TotalDebt).HasPrecision(18, 2);
            entity.HasOne(k => k.OwnerAdmin).WithMany().HasForeignKey(k => k.OwnerAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AdminManagementExpense>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ExternalId).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PurchaseDate).HasMaxLength(20).IsRequired();
            entity.Property(e => e.InvoicePhotoUrl).HasColumnType("longtext");
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.HasOne(e => e.OwnerAdmin).WithMany().HasForeignKey(e => e.OwnerAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SharedUtilityService>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).ValueGeneratedOnAdd();
            entity.Property(s => s.ExternalId).HasMaxLength(64).IsRequired();
            entity.HasIndex(s => s.ExternalId).IsUnique();
            entity.Property(s => s.Type).HasMaxLength(40).IsRequired();
            entity.Property(s => s.Month).HasMaxLength(10);
            entity.Property(s => s.Amount).HasPrecision(18, 2);
            entity.Property(s => s.PerResidentShare).HasPrecision(18, 2);
            entity.HasOne(s => s.OwnerAdmin).WithMany().HasForeignKey(s => s.OwnerAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FixedPayoutRecipient>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).ValueGeneratedOnAdd();
            entity.Property(r => r.ExternalId).HasMaxLength(64).IsRequired();
            entity.HasIndex(r => r.ExternalId).IsUnique();
            entity.Property(r => r.Name).HasMaxLength(200).IsRequired();
            entity.Property(r => r.Dni).HasMaxLength(80).IsRequired();
            entity.Property(r => r.Phone).HasMaxLength(40).IsRequired();
            entity.Property(r => r.NextPaymentDate).HasMaxLength(20).IsRequired();
            entity.Property(r => r.PhotoUrl).HasColumnType("longtext");
            entity.Property(r => r.PaymentHistoryJson).HasColumnType("longtext");
            entity.Property(r => r.CreatedAtIso).HasMaxLength(40);
            entity.Property(r => r.Salary).HasPrecision(18, 2);
            entity.HasOne(r => r.OwnerAdmin).WithMany().HasForeignKey(r => r.OwnerAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
