using BuildingFex.Api.Iam.Domain.Model;
using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Iam.Interfaces.Rest.Resources;

namespace BuildingFex.Api.Iam.Interfaces.Rest.Transform;

public static class UserResourceAssembler
{
    public static UserResource ToResource(User user) =>
        new(
            user.ExternalId,
            user.Name,
            user.Email,
            user.Role,
            user.Floor,
            user.Code,
            user.OwnerAdmin?.ExternalId,
            user.Dni,
            user.Address,
            user.Company,
            user.Ruc,
            user.AdmissionDate?.ToString("yyyy-MM-dd"),
            user.Role == "resident"
                ? !string.IsNullOrEmpty(user.Email) && !string.IsNullOrEmpty(user.PasswordHash)
                : null,
            user.Role == "admin" ? SubscriptionPlans.Normalize(user.SubscriptionPlanId) : null,
            user.Role == "admin" && user.SubscriptionPaidUntil.HasValue
                ? user.SubscriptionPaidUntil.Value.ToString("o")
                : null,
            user.Role == "admin" ? SubscriptionPlans.MaxResidents(user.SubscriptionPlanId) : null);
}
