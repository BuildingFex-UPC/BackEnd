using BuildingFex.Api.Incidents.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace BuildingFex.Api.Incidents.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyIncidentsConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Incident>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Id).ValueGeneratedOnAdd();
            entity.Property(i => i.ExternalId).HasMaxLength(64).IsRequired();
            entity.HasIndex(i => i.ExternalId).IsUnique();
            entity.Property(i => i.ResidentExternalId).HasMaxLength(64);
            entity.Property(i => i.ResidentName).HasMaxLength(200);
            entity.Property(i => i.Description).HasMaxLength(2000).IsRequired();
            entity.Property(i => i.Status).HasMaxLength(20).IsRequired();
            entity.Property(i => i.Provider).HasMaxLength(200);

            entity.HasOne(i => i.OwnerAdmin)
                .WithMany()
                .HasForeignKey(i => i.OwnerAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
