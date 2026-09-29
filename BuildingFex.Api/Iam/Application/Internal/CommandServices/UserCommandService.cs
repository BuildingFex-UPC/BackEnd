using BuildingFex.Api.Iam.Application.CommandServices;
using BuildingFex.Api.Iam.Application.Internal.OutboundServices;
using BuildingFex.Api.Iam.Domain.Model;
using BuildingFex.Api.Iam.Domain.Model.Aggregates;
using BuildingFex.Api.Iam.Domain.Model.Commands;
using BuildingFex.Api.Iam.Domain.Repositories;
using BuildingFex.Api.Shared.Application.Model;
using BuildingFex.Api.Shared.Domain.Repositories;

namespace BuildingFex.Api.Iam.Application.Internal.CommandServices;

public class UserCommandService(
    IUserRepository userRepository,
    ITokenService tokenService,
    IHashingService hashingService,
    IUnitOfWork unitOfWork) : IUserCommandService
{
    public async Task<Result<(User user, string token)>> Handle(
        SignInCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByEmailAsync(command.Email, cancellationToken);

        if (user is null)
            return Result<(User user, string token)>.Failure(
                IamError.EmailNotFound,
                "No existe una cuenta con ese email.");

        if (!hashingService.VerifyPassword(command.Password, user.PasswordHash))
            return Result<(User user, string token)>.Failure(
                IamError.InvalidPassword,
                "Contraseña incorrecta.");

        var token = tokenService.GenerateToken(user);
        return Result<(User user, string token)>.Success((user, token));
    }

    public async Task<Result<(User user, string token)>> Handle(
        RegisterAdminCommand command,
        CancellationToken cancellationToken = default)
    {
        if (await userRepository.ExistsByEmailAsync(command.Email, cancellationToken))
            return Result<(User user, string token)>.Failure(
                IamError.EmailAlreadyExists,
                "El email ya está registrado.");

        var externalId = $"admin-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var user = User.CreateAdmin(
            externalId,
            command.Name,
            command.Email,
            hashingService.HashPassword(command.Password),
            command.Dni,
            command.Address,
            command.Company,
            command.Ruc);

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        var token = tokenService.GenerateToken(user);
        return Result<(User user, string token)>.Success((user, token));
    }

    public async Task<Result<User>> Handle(
        CreateResidentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name) ||
            string.IsNullOrWhiteSpace(command.Floor) ||
            string.IsNullOrWhiteSpace(command.Code))
            return Result<User>.Failure(
                IamError.ResidentFieldsRequired,
                "Nombre, piso y código son obligatorios.");

        if (string.IsNullOrWhiteSpace(command.OwnerAdminExternalId))
            return Result<User>.Failure(
                IamError.ResidentOwnerRequired,
                "ownerAdminId es obligatorio.");

        var owner = await userRepository.FindByExternalIdAsync(command.OwnerAdminExternalId, cancellationToken);
        if (owner is null || owner.Role != "admin")
            return Result<User>.Failure(
                IamError.ResidentOwnerRequired,
                "Administrador no encontrado.");

        if (await userRepository.ExistsResidentByCodeAsync(command.Code, owner.Id, cancellationToken))
            return Result<User>.Failure(
                IamError.ResidentCodeAlreadyExists,
                "Ya existe un residente con ese código.");

        var residentCount = await userRepository.CountResidentsByOwnerAdminIdAsync(owner.Id, cancellationToken);
        var planLimit = SubscriptionPlans.MaxResidents(owner.SubscriptionPlanId);
        if (residentCount >= planLimit)
            return Result<User>.Failure(
                IamError.ResidentPlanLimitReached,
                $"Límite del plan alcanzado (máximo {planLimit} residentes). Cambia tu plan en Ajustes.");

        DateOnly? admissionDate = null;
        if (!string.IsNullOrWhiteSpace(command.AdmissionDate) &&
            DateOnly.TryParse(command.AdmissionDate, out var parsed))
            admissionDate = parsed;

        var externalId = string.IsNullOrWhiteSpace(command.ExternalId)
            ? $"resident-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
            : command.ExternalId.Trim();

        var user = User.CreateResident(
            externalId,
            command.Name,
            email: null,
            passwordHash: string.Empty,
            command.Floor,
            command.Code,
            owner.Id,
            admissionDate);

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        var created = await userRepository.FindByExternalIdAsync(externalId, cancellationToken);
        return Result<User>.Success(created!);
    }

    public async Task<Result<User>> Handle(
        UpdateResidentCredentialsCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByExternalIdAsync(command.ExternalId, cancellationToken);
        if (user is null || user.Role != "resident")
            return Result<User>.Failure(
                IamError.ResidentNotFound,
                "Residente no encontrado.");

        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Password))
            return Result<User>.Failure(
                IamError.ResidentFieldsRequired,
                "Email y contraseña son obligatorios.");

        var emailOwner = await userRepository.FindByEmailAsync(command.Email, cancellationToken);
        if (emailOwner is not null && emailOwner.ExternalId != user.ExternalId)
            return Result<User>.Failure(
                IamError.EmailAlreadyExists,
                "El email ya está registrado.");

        user.UpdateCredentials(command.Email, hashingService.HashPassword(command.Password));
        userRepository.Update(user);
        await unitOfWork.CompleteAsync(cancellationToken);

        var updated = await userRepository.FindByExternalIdAsync(command.ExternalId, cancellationToken);
        return Result<User>.Success(updated!);
    }

    public async Task<Result<User>> Handle(
        DeleteResidentCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByExternalIdAsync(command.ExternalId, cancellationToken);
        if (user is null || user.Role != "resident")
            return Result<User>.Failure(
                IamError.ResidentNotFound,
                "Residente no encontrado.");

        userRepository.Remove(user);
        await unitOfWork.CompleteAsync(cancellationToken);

        return Result<User>.Success(user);
    }
}
