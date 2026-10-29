using System.Text.Json.Serialization;
using BuildingFex.Api.Iam.Domain.Model;
using BuildingFex.Api.Shared.Domain.Model.Entities;

namespace BuildingFex.Api.Iam.Domain.Model.Aggregates;

/// <summary>
/// Aggregate root de IAM. Unifica admin y residente (como en db.json del frontend).
/// </summary>
public class User : IAuditableEntity
{
    public int Id { get; private set; }

    /// <summary>ID string compatible con json-server (ej. admin-seed-1).</summary>
    public string ExternalId { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    [JsonIgnore]
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>admin | resident</summary>
    public string Role { get; private set; } = "resident";

    // Admin
    public string? Dni { get; private set; }
    public string? Address { get; private set; }
    public string? Company { get; private set; }
    public string? Ruc { get; private set; }

    // Resident
    public string? Floor { get; private set; }
    public string? Code { get; private set; }
    public DateOnly? AdmissionDate { get; private set; }

    /// <summary>
    /// Profile icon shown in the app drawer. Two shapes are accepted:
    /// a preset ("preset|😀|#FF5722") or an uploaded photo as a data URL
    /// ("data:image/jpeg;base64,..."). Null = default person icon.
    /// </summary>
    public string? Avatar { get; private set; }

    /// <summary>Multi-tenancy: residente pertenece a este admin (FK interna).</summary>
    public int? OwnerAdminId { get; private set; }

    public User? OwnerAdmin { get; private set; }

    /// <summary>Subscription tier for admin accounts (free, essential, standard, scale).</summary>
    public string SubscriptionPlanId { get; private set; } = SubscriptionPlanIds.Free;

    /// <summary>When the current paid period ends (UTC). Null for free or legacy rows.</summary>
    public DateTimeOffset? SubscriptionPaidUntil { get; private set; }

    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private User()
    {
    }

    public static User CreateAdmin(
        string externalId,
        string name,
        string email,
        string passwordHash,
        string? dni = null,
        string? address = null,
        string? company = null,
        string? ruc = null)
    {
        return new User
        {
            ExternalId = externalId.Trim(),
            Name = name.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = "admin",
            Dni = dni,
            Address = address,
            Company = company,
            Ruc = ruc,
        };
    }

    public static User CreateResident(
        string externalId,
        string name,
        string? email,
        string? passwordHash,
        string floor,
        string code,
        int ownerAdminId,
        DateOnly? admissionDate = null)
    {
        return new User
        {
            ExternalId = externalId.Trim(),
            Name = name.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash ?? string.Empty,
            Role = "resident",
            Floor = floor,
            Code = code,
            OwnerAdminId = ownerAdminId,
            AdmissionDate = admissionDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
        };
    }

    public void UpdateCredentials(string email, string passwordHash)
    {
        if (Role != "resident")
            throw new InvalidOperationException("Only residents can update credentials.");

        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
    }

    /// <summary>
    /// Actualiza los datos editables del perfil: nombre, DNI, dirección,
    /// correo de acceso e icono de perfil. Los residentes también actualizan
    /// piso; los admins, empresa y RUC. El correo sí es editable aquí
    /// (validando unicidad en el servicio) porque es el identificador de
    /// login; el código de departamento lo asigna el admin.
    /// </summary>
    public void UpdateProfile(
        string name,
        string? dni,
        string? address,
        string? floor = null,
        string? email = null,
        string? avatar = null,
        string? company = null,
        string? ruc = null)
    {
        Name = name.Trim();
        Dni = Normalize(dni);
        Address = Normalize(address);
        Floor = Normalize(floor);
        Company = Normalize(company);
        Ruc = Normalize(ruc);

        if (email is not null)
            Email = email.Trim().ToLowerInvariant();

        // Avatar may be intentionally cleared, so only trim (keep null as null).
        Avatar = avatar is null ? null : avatar.Trim();
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void UpdateSubscription(string planId, DateTimeOffset? paidUntil = null)
    {
        if (Role != "admin")
            throw new InvalidOperationException("Only admin accounts have subscription plans.");

        SubscriptionPlanId = SubscriptionPlans.Normalize(planId);
        SubscriptionPaidUntil = SubscriptionPlans.IsPaid(SubscriptionPlanId) ? paidUntil : null;
    }
}
