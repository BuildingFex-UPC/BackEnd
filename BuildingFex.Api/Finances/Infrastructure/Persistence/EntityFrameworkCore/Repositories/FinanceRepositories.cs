using BuildingFex.Api.Finances.Domain.Model.Aggregates;
using BuildingFex.Api.Finances.Domain.Repositories;
using BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using BuildingFex.Api.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BuildingFex.Api.Finances.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class FeeRepository(AppDbContext context) : BaseRepository<Fee>(context), IFeeRepository
{
    private IQueryable<Fee> WithOwner() => Context.Set<Fee>().Include(f => f.OwnerAdmin);

    public async Task<Fee?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return null;

        return await WithOwner()
            .FirstOrDefaultAsync(f => f.ExternalId == externalId.Trim(), cancellationToken);
    }

    public async Task<IEnumerable<Fee>> ListAsync(
        string? ownerAdminExternalId,
        string? residentExternalId,
        CancellationToken cancellationToken = default)
    {
        var query = WithOwner().AsQueryable();

        if (!string.IsNullOrWhiteSpace(ownerAdminExternalId))
            query = query.Where(f => f.OwnerAdmin!.ExternalId == ownerAdminExternalId.Trim());

        if (!string.IsNullOrWhiteSpace(residentExternalId))
            query = query.Where(f => f.ResidentExternalId == residentExternalId.Trim());

        return await query.OrderBy(f => f.DueDate).ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => await Context.Set<Fee>().AnyAsync(cancellationToken);
}

public class PaymentRepository(AppDbContext context) : BaseRepository<Payment>(context), IPaymentRepository
{
    private IQueryable<Payment> WithOwner() => Context.Set<Payment>().Include(p => p.OwnerAdmin);

    public async Task<Payment?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return null;

        return await WithOwner()
            .FirstOrDefaultAsync(p => p.ExternalId == externalId.Trim(), cancellationToken);
    }

    public async Task<IEnumerable<Payment>> ListAsync(
        string? ownerAdminExternalId,
        string? residentExternalId,
        CancellationToken cancellationToken = default)
    {
        var query = WithOwner().AsQueryable();

        if (!string.IsNullOrWhiteSpace(ownerAdminExternalId))
            query = query.Where(p => p.OwnerAdmin!.ExternalId == ownerAdminExternalId.Trim());

        if (!string.IsNullOrWhiteSpace(residentExternalId))
            query = query.Where(p => p.ResidentExternalId == residentExternalId.Trim());

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => await Context.Set<Payment>().AnyAsync(cancellationToken);
}

public class ReceiptRepository(AppDbContext context) : BaseRepository<Receipt>(context), IReceiptRepository
{
    private IQueryable<Receipt> WithOwner() => Context.Set<Receipt>().Include(r => r.OwnerAdmin);

    public async Task<Receipt?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return null;

        return await WithOwner()
            .FirstOrDefaultAsync(r => r.ExternalId == externalId.Trim(), cancellationToken);
    }

    public async Task<IEnumerable<Receipt>> ListAsync(
        string? ownerAdminExternalId,
        string? residentExternalId,
        CancellationToken cancellationToken = default)
    {
        var query = WithOwner().AsQueryable();

        if (!string.IsNullOrWhiteSpace(ownerAdminExternalId))
            query = query.Where(r => r.OwnerAdmin!.ExternalId == ownerAdminExternalId.Trim());

        if (!string.IsNullOrWhiteSpace(residentExternalId))
            query = query.Where(r => r.ResidentExternalId == residentExternalId.Trim());

        return await query.OrderByDescending(r => r.DueDate).ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => await Context.Set<Receipt>().AnyAsync(cancellationToken);
}

public class FinanceSettingRepository(AppDbContext context)
    : BaseRepository<FinanceSetting>(context), IFinanceSettingRepository
{
    private IQueryable<FinanceSetting> WithOwner() =>
        Context.Set<FinanceSetting>().Include(s => s.OwnerAdmin);

    public async Task<FinanceSetting?> FindByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return null;

        return await WithOwner()
            .FirstOrDefaultAsync(s => s.ExternalId == externalId.Trim(), cancellationToken);
    }

    public async Task<IEnumerable<FinanceSetting>> ListAsync(
        string? ownerAdminExternalId,
        CancellationToken cancellationToken = default)
    {
        var query = WithOwner().AsQueryable();

        if (!string.IsNullOrWhiteSpace(ownerAdminExternalId))
            query = query.Where(s => s.OwnerAdmin!.ExternalId == ownerAdminExternalId.Trim());

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => await Context.Set<FinanceSetting>().AnyAsync(cancellationToken);
}

public class KpiRepository(AppDbContext context) : BaseRepository<KpiRecord>(context), IKpiRepository
{
    private IQueryable<KpiRecord> WithOwner() => Context.Set<KpiRecord>().Include(k => k.OwnerAdmin);

    public async Task<KpiRecord?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return null;

        return await WithOwner()
            .FirstOrDefaultAsync(k => k.ExternalId == externalId.Trim(), cancellationToken);
    }

    public async Task<IEnumerable<KpiRecord>> ListAsync(
        string? ownerAdminExternalId,
        CancellationToken cancellationToken = default)
    {
        var query = WithOwner().AsQueryable();

        if (!string.IsNullOrWhiteSpace(ownerAdminExternalId))
            query = query.Where(k => k.OwnerAdmin!.ExternalId == ownerAdminExternalId.Trim());

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => await Context.Set<KpiRecord>().AnyAsync(cancellationToken);
}

public class AdminManagementExpenseRepository(AppDbContext context)
    : BaseRepository<AdminManagementExpense>(context), IAdminManagementExpenseRepository
{
    private IQueryable<AdminManagementExpense> WithOwner() =>
        Context.Set<AdminManagementExpense>().Include(e => e.OwnerAdmin);

    public async Task<IEnumerable<AdminManagementExpense>> ListAsync(
        string? ownerAdminExternalId,
        CancellationToken cancellationToken = default)
    {
        var query = WithOwner().AsQueryable();

        if (!string.IsNullOrWhiteSpace(ownerAdminExternalId))
            query = query.Where(e => e.OwnerAdmin!.ExternalId == ownerAdminExternalId.Trim());

        return await query.OrderByDescending(e => e.PurchaseDate).ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => await Context.Set<AdminManagementExpense>().AnyAsync(cancellationToken);
}

public class SharedUtilityServiceRepository(AppDbContext context)
    : BaseRepository<SharedUtilityService>(context), ISharedUtilityServiceRepository
{
    private IQueryable<SharedUtilityService> WithOwner() =>
        Context.Set<SharedUtilityService>().Include(s => s.OwnerAdmin);

    public async Task<IEnumerable<SharedUtilityService>> ListAsync(
        string? ownerAdminExternalId,
        CancellationToken cancellationToken = default)
    {
        var query = WithOwner().AsQueryable();

        if (!string.IsNullOrWhiteSpace(ownerAdminExternalId))
            query = query.Where(s => s.OwnerAdmin!.ExternalId == ownerAdminExternalId.Trim());

        return await query.OrderByDescending(s => s.Id).ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => await Context.Set<SharedUtilityService>().AnyAsync(cancellationToken);
}

public class FixedPayoutRecipientRepository(AppDbContext context)
    : BaseRepository<FixedPayoutRecipient>(context), IFixedPayoutRecipientRepository
{
    private IQueryable<FixedPayoutRecipient> WithOwner() =>
        Context.Set<FixedPayoutRecipient>().Include(r => r.OwnerAdmin);

    public async Task<FixedPayoutRecipient?> FindByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return null;

        return await WithOwner()
            .FirstOrDefaultAsync(r => r.ExternalId == externalId.Trim(), cancellationToken);
    }

    public async Task<IEnumerable<FixedPayoutRecipient>> ListAsync(
        string? ownerAdminExternalId,
        CancellationToken cancellationToken = default)
    {
        var query = WithOwner().AsQueryable();

        if (!string.IsNullOrWhiteSpace(ownerAdminExternalId))
            query = query.Where(r => r.OwnerAdmin!.ExternalId == ownerAdminExternalId.Trim());

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => await Context.Set<FixedPayoutRecipient>().AnyAsync(cancellationToken);
}
