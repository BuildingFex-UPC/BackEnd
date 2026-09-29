using BuildingFex.Api.Information.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace BuildingFex.Api.Information.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyInformationConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Announcement>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).ValueGeneratedOnAdd();
            entity.Property(a => a.ExternalId).HasMaxLength(64).IsRequired();
            entity.HasIndex(a => a.ExternalId).IsUnique();
            entity.Property(a => a.Title).HasMaxLength(500).IsRequired();
            entity.Property(a => a.Body).HasMaxLength(4000);
            entity.Property(a => a.Priority).HasMaxLength(20).IsRequired();
            entity.Property(a => a.Duration).IsRequired();
            entity.Property(a => a.AuthorName).HasMaxLength(200);

            entity.HasOne(a => a.OwnerAdmin)
                .WithMany()
                .HasForeignKey(a => a.OwnerAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
