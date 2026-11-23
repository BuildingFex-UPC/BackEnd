using BuildingFex.Api.Notifications.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace BuildingFex.Api.Notifications.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyNotificationsConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Id).ValueGeneratedOnAdd();
            entity.Property(n => n.ExternalId).HasMaxLength(128).IsRequired();
            entity.HasIndex(n => n.ExternalId).IsUnique();
            entity.Property(n => n.RecipientExternalId).HasMaxLength(128).IsRequired();
            entity.Property(n => n.RecipientRole).HasMaxLength(32).IsRequired();
            entity.Property(n => n.Title).HasMaxLength(256).IsRequired();
            entity.Property(n => n.Message).HasColumnType("text").IsRequired();
            entity.Property(n => n.Type).HasMaxLength(64).IsRequired();
            entity.Property(n => n.RelatedEntityId).HasMaxLength(128);
            entity.Property(n => n.IsRead).IsRequired();
            entity.Property(n => n.CreatedAt).IsRequired();
            entity.HasIndex(n => new { n.RecipientExternalId, n.IsRead });
        });
    }
}

