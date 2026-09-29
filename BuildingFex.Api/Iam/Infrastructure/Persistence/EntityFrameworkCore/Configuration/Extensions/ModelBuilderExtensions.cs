using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using BuildingFex.Api.Iam.Domain.Model;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BuildingFex.Api.Iam.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyIamConfiguration(this ModelBuilder builder)
    {
        // DateOnly.ToDateTime() yields Kind=Unspecified, which PostgreSQL rejects for
        // timestamptz columns. The column holds a date with no time, so midnight UTC is
        // the canonical representation and round-trips back to the same DateOnly.
        var dateOnlyConverter = new ValueConverter<DateOnly?, DateTime?>(
            v => v.HasValue
                ? DateTime.SpecifyKind(v.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
                : null,
            v => v.HasValue ? DateOnly.FromDateTime(v.Value) : null);

        builder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).ValueGeneratedOnAdd();
            entity.Property(u => u.ExternalId).HasMaxLength(64).IsRequired();
            entity.HasIndex(u => u.ExternalId).IsUnique();
            entity.Property(u => u.Name).HasMaxLength(200).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(320).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(20).IsRequired();
            entity.Property(u => u.Dni).HasMaxLength(50);
            entity.Property(u => u.Company).HasMaxLength(200);
            entity.Property(u => u.Ruc).HasMaxLength(20);
            entity.Property(u => u.Floor).HasMaxLength(10);
            entity.Property(u => u.Code).HasMaxLength(20);
            entity.Property(u => u.AdmissionDate).HasConversion(dateOnlyConverter);
            entity.Property(u => u.SubscriptionPlanId).HasMaxLength(20).HasDefaultValue(SubscriptionPlanIds.Free);
            entity.Property(u => u.SubscriptionPaidUntil);

            entity.HasOne(u => u.OwnerAdmin)
                .WithMany()
                .HasForeignKey(u => u.OwnerAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
