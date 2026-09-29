using System.Security.Claims;
using BuildingFex.Api.Finances.Application.Internal.MercadoPago;
using BuildingFex.Api.Iam.Domain.Model;
using BuildingFex.Api.Iam.Domain.Repositories;
using BuildingFex.Api.Iam.Interfaces.Rest.Resources;
using BuildingFex.Api.Shared.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingFex.Api.Iam.Interfaces.Rest;

[ApiController]
[Route("api/v1/subscription")]
[Authorize]
public class SubscriptionController(
    IUserRepository userRepository,
    IMercadoPagoService mercadoPagoService,
    IUnitOfWork unitOfWork,
    ILogger<SubscriptionController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCurrent(CancellationToken ct)
    {
        var admin = await ResolveCurrentAdminAsync(ct);
        if (admin is null)
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Solo administradores." });

        return Ok(await BuildResponseAsync(admin, ct));
    }

    [HttpPatch]
    public async Task<IActionResult> ChangePlan(
        [FromBody] ChangeSubscriptionPlanResource resource,
        CancellationToken ct)
    {
        var admin = await ResolveCurrentAdminAsync(ct);
        if (admin is null)
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Solo administradores." });

        var planId = SubscriptionPlans.Normalize(resource.PlanId);
        if (SubscriptionPlans.IsPaid(planId))
            return BadRequest(new
            {
                code = "PAID_PLAN_REQUIRES_CHECKOUT",
                message = "Los planes de pago requieren checkout con Mercado Pago.",
            });

        var residentsCount = await userRepository.CountResidentsByOwnerAdminIdAsync(admin.Id, ct);
        var newLimit = SubscriptionPlans.MaxResidents(planId);
        if (residentsCount > newLimit)
        {
            return BadRequest(new
            {
                code = "PLAN_DOWNGRADE_RESIDENTS_EXCEEDED",
                message = $"Tienes {residentsCount} residentes; el plan {planId} permite máximo {newLimit}.",
                residentsCount,
                residentLimit = newLimit,
            });
        }

        admin.UpdateSubscription(planId, null);
        userRepository.Update(admin);
        await unitOfWork.CompleteAsync(ct);

        return Ok(await BuildResponseAsync(admin, ct));
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(
        [FromBody] SubscriptionCheckoutResource resource,
        CancellationToken ct)
    {
        var admin = await ResolveCurrentAdminAsync(ct);
        if (admin is null)
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Solo administradores." });

        var planId = SubscriptionPlans.Normalize(resource.PlanId);
        if (!SubscriptionPlans.IsPaid(planId))
            return BadRequest(new { code = "FREE_PLAN_NO_CHECKOUT", message = "El plan free no requiere pago." });

        try
        {
            var frontendBaseUrl = resource.FrontendBaseUrl;
            if (string.IsNullOrWhiteSpace(frontendBaseUrl))
            {
                frontendBaseUrl = Request.Headers.Origin.FirstOrDefault()
                    ?? Request.Headers.Referer.FirstOrDefault();
            }

            var result = await mercadoPagoService.CreateSubscriptionPreferenceAsync(
                new CreateSubscriptionPreferenceRequest(
                    admin.ExternalId,
                    planId,
                    admin.Email,
                    frontendBaseUrl),
                ct);

            var demo = string.Equals(result.PreferenceId, "DEMO", StringComparison.OrdinalIgnoreCase);
            return Ok(new
            {
                preferenceId = result.PreferenceId,
                initPoint = result.InitPoint,
                demo,
                amount = SubscriptionPlans.MonthlyPricePen(planId),
                planId,
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Subscription checkout rejected for admin {AdminId} plan {PlanId}", admin.ExternalId, planId);
            return BadRequest(new { code = "CHECKOUT_ERROR", message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Subscription checkout failed for admin {AdminId} plan {PlanId}", admin.ExternalId, planId);
            var detail = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, new { code = "CHECKOUT_ERROR", message = detail });
        }
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm(
        [FromBody] ConfirmSubscriptionResource resource,
        CancellationToken ct)
    {
        var admin = await ResolveCurrentAdminAsync(ct);
        if (admin is null)
            return Unauthorized(new { code = "UNAUTHORIZED", message = "Solo administradores." });

        var planId = SubscriptionPlans.Normalize(resource.PlanId);

        try
        {
            var allowDemo = resource.Demo;
            var result = await mercadoPagoService.ConfirmSubscriptionPaymentAsync(
                admin.ExternalId,
                planId,
                resource.PaymentId,
                allowDemo,
                ct);

            var refreshed = await userRepository.FindByExternalIdAsync(admin.ExternalId, ct);
            return Ok(new
            {
                activated = result.Activated,
                planId = result.PlanId,
                paidUntil = result.PaidUntil,
                subscription = refreshed is null ? null : await BuildResponseAsync(refreshed, ct),
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Subscription confirm failed for admin {AdminId}", admin.ExternalId);
            return BadRequest(new { code = "CONFIRM_FAILED", message = ex.Message });
        }
    }

    private async Task<BuildingFex.Api.Iam.Domain.Model.Aggregates.User?> ResolveCurrentAdminAsync(CancellationToken ct)
    {
        var externalId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(externalId))
            return null;

        var user = await userRepository.FindByExternalIdAsync(externalId, ct);
        if (user is null || user.Role != "admin")
            return null;

        return user;
    }

    private async Task<SubscriptionResponseResource> BuildResponseAsync(
        BuildingFex.Api.Iam.Domain.Model.Aggregates.User admin,
        CancellationToken ct)
    {
        var planId = SubscriptionPlans.Normalize(admin.SubscriptionPlanId);
        var residentsCount = await userRepository.CountResidentsByOwnerAdminIdAsync(admin.Id, ct);

        return new SubscriptionResponseResource(
            planId,
            SubscriptionPlans.MaxResidents(planId),
            residentsCount,
            SubscriptionPlans.MonthlyPricePen(planId),
            admin.SubscriptionPaidUntil?.ToString("o"),
            SubscriptionPlans.IsPaid(planId));
    }
}
