using BuildingFex.Api.Finances.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using BuildingFex.Api.Iam.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using BuildingFex.Api.Incidents.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using BuildingFex.Api.Information.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using BuildingFex.Api.SocialSpaces.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using BuildingFex.Api.Support.Domain.Model.Aggregates;
using BuildingFex.Api.Import.Domain.Model.Aggregates;
using BuildingFex.Api.Team.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
/// DbContext compartido por todos los bounded contexts de BuildingFex.
/// Cada módulo registra sus tablas en OnModelCreating via Apply*Configuration().
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Registrar configuraciones por bounded context
        builder.ApplyIamConfiguration();
        builder.ApplyIncidentsConfiguration();
        builder.ApplyFinancesConfiguration();
        builder.ApplySocialSpacesConfiguration();
        builder.ApplyInformationConfiguration();

        // Support chats
        builder.Entity<SupportChat>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).ValueGeneratedOnAdd();
            entity.Property(s => s.ExternalId).HasMaxLength(128).IsRequired();
            entity.HasIndex(s => s.ExternalId).IsUnique();
            entity.Property(s => s.OwnerAdminId).HasMaxLength(128).IsRequired();
            entity.Property(s => s.ResidentId).HasMaxLength(128).IsRequired();
            entity.Property(s => s.ResidentName).HasMaxLength(256);
            entity.Property(s => s.Topic).HasMaxLength(256);
            entity.Property(s => s.Status).HasMaxLength(32);
            entity.Property(s => s.MessagesJson).HasColumnType("LONGTEXT");
        });

        // Import uploads
        builder.Entity<ImportUpload>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).ValueGeneratedOnAdd();
            entity.Property(u => u.ExternalId).HasMaxLength(128).IsRequired();
            entity.HasIndex(u => u.ExternalId).IsUnique();
            entity.Property(u => u.OwnerAdminId).HasMaxLength(128).IsRequired();
            entity.Property(u => u.FileName).HasMaxLength(512);
            entity.Property(u => u.MimeType).HasMaxLength(128);
            entity.Property(u => u.DataUrl).HasColumnType("LONGTEXT");
        });

        // Team workers
        builder.Entity<TeamWorker>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.Property(w => w.Id).ValueGeneratedOnAdd();
            entity.Property(w => w.ExternalId).HasMaxLength(128).IsRequired();
            entity.HasIndex(w => w.ExternalId).IsUnique();
            entity.Property(w => w.OwnerAdminId).HasMaxLength(128).IsRequired();
            entity.Property(w => w.Name).HasMaxLength(256);
            entity.Property(w => w.Phone).HasMaxLength(64);
            entity.Property(w => w.Dni).HasMaxLength(32);
            entity.Property(w => w.Salary).HasPrecision(18, 2);
            entity.Property(w => w.PhotoUrl).HasColumnType("LONGTEXT");
        });

        builder.UseSnakeCaseNamingConvention();
    }
}
