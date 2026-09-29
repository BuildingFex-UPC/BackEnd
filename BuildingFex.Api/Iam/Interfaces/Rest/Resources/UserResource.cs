namespace BuildingFex.Api.Iam.Interfaces.Rest.Resources;

public record UserResource(
    string Id,
    string Name,
    string Email,
    string Role,
    string? Floor,
    string? Code,
    string? OwnerAdminId,
    string? Dni,
    string? Address,
    string? Company,
    string? Ruc,
    string? AdmissionDate,
    bool? HasCredentials = null,
    string? SubscriptionPlanId = null,
    string? SubscriptionPaidUntil = null,
    int? ResidentLimit = null);

public record AuthenticatedUserResource(UserResource User, string Token);

public record SignInResource(string Email, string Password);

public record RegisterAdminResource(
    string Name,
    string Email,
    string Password,
    string? Dni,
    string? Address,
    string? Company,
    string? Ruc);

public record CreateUserCompatResource(
    string? Id,
    string? Name,
    string? Floor,
    string? Code,
    string? Email,
    string? Password,
    string? Role,
    string? AdmissionDate,
    string? OwnerAdminId,
    string? Dni = null,
    string? Address = null,
    string? Company = null,
    string? Ruc = null);

public record UpdateResidentCredentialsResource(string? Email, string? Password);

public record SetResidentCredentialsByCodeResource(string Code, string Email, string Password);

public record SubscriptionResponseResource(
    string PlanId,
    int ResidentLimit,
    int ResidentsCount,
    decimal MonthlyPricePen,
    string? PaidUntil,
    bool IsPaid);

public record ChangeSubscriptionPlanResource(string PlanId);

public record SubscriptionCheckoutResource(string PlanId, string? FrontendBaseUrl = null);

public record ConfirmSubscriptionResource(string PlanId, long? PaymentId, bool Demo = false);
