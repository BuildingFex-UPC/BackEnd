namespace BuildingFex.Api.Iam.Domain.Model;

public enum IamError
{
    InvalidCredentials,
    EmailNotFound,
    InvalidPassword,
    EmailAlreadyExists,
    UserNotFound,
    ResidentFieldsRequired,
    ResidentCodeAlreadyExists,
    ResidentOwnerRequired,
    ResidentNotFound,
    ResidentPlanLimitReached,
}
