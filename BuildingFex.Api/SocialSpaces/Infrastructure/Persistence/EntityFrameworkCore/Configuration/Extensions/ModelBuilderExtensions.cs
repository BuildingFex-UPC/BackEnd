using BuildingFex.Api.SocialSpaces.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace BuildingFex.Api.SocialSpaces.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplySocialSpacesConfiguration(this ModelBuilder builder)
    {
        builder.Entity<SocialSpace>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).ValueGeneratedOnAdd();
            entity.Property(s => s.ExternalId).HasMaxLength(64).IsRequired();
            entity.HasIndex(s => s.ExternalId).IsUnique();
            entity.Property(s => s.Name).HasMaxLength(200).IsRequired();
            entity.Property(s => s.Description).HasMaxLength(2000);
            entity.Property(s => s.ImageUrl).HasColumnType("longtext");
            entity.HasOne(s => s.OwnerAdmin)
                .WithMany()
                .HasForeignKey(s => s.OwnerAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Reservation>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).ValueGeneratedOnAdd();
            entity.Property(r => r.ExternalId).HasMaxLength(64).IsRequired();
            entity.HasIndex(r => r.ExternalId).IsUnique();
            entity.Property(r => r.SpaceExternalId).HasMaxLength(64).IsRequired();
            entity.Property(r => r.ResidentExternalId).HasMaxLength(64).IsRequired();
            entity.Property(r => r.ResidentName).HasMaxLength(200);
            entity.Property(r => r.ResidentCode).HasMaxLength(20);
            entity.Property(r => r.Date).HasMaxLength(20).IsRequired();
            entity.Property(r => r.StartTime).HasMaxLength(10).IsRequired();
            entity.Property(r => r.EndTime).HasMaxLength(10).IsRequired();
            entity.Property(r => r.GuestsJson).HasColumnType("longtext");
            entity.Property(r => r.GuestInviteToken).HasMaxLength(64);
            entity.HasIndex(r => r.GuestInviteToken);
            entity.HasOne(r => r.OwnerAdmin)
                .WithMany()
                .HasForeignKey(r => r.OwnerAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
