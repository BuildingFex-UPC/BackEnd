using BuildingFex.Api.Finances.Domain.Model.Aggregates;

namespace BuildingFex.Api.Finances.Domain.Repositories;

public interface IFeeRepository
{
    Task<Fee?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Fee>> ListAsync(string? ownerAdminExternalId, string? residentExternalId, CancellationToken cancellationToken = default);
    Task AddAsync(Fee entity, CancellationToken cancellationToken = default);
    void Update(Fee entity);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}

public interface IPaymentRepository
{
    Task<Payment?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> ListAsync(string? ownerAdminExternalId, string? residentExternalId, CancellationToken cancellationToken = default);
    Task AddAsync(Payment entity, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}

public interface IReceiptRepository
{
    Task<Receipt?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Receipt>> ListAsync(string? ownerAdminExternalId, string? residentExternalId, CancellationToken cancellationToken = default);
    Task AddAsync(Receipt entity, CancellationToken cancellationToken = default);
    void Update(Receipt entity);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}

public interface IFinanceSettingRepository
{
    Task<FinanceSetting?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<FinanceSetting>> ListAsync(string? ownerAdminExternalId, CancellationToken cancellationToken = default);
    Task AddAsync(FinanceSetting entity, CancellationToken cancellationToken = default);
    void Update(FinanceSetting entity);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}

public interface IKpiRepository
{
    Task<KpiRecord?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<KpiRecord>> ListAsync(string? ownerAdminExternalId, CancellationToken cancellationToken = default);
    Task AddAsync(KpiRecord entity, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}

public interface IAdminManagementExpenseRepository
{
    Task<IEnumerable<AdminManagementExpense>> ListAsync(string? ownerAdminExternalId, CancellationToken cancellationToken = default);
    Task AddAsync(AdminManagementExpense entity, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}

public interface ISharedUtilityServiceRepository
{
    Task<IEnumerable<SharedUtilityService>> ListAsync(string? ownerAdminExternalId, CancellationToken cancellationToken = default);
    Task AddAsync(SharedUtilityService entity, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}

public interface IFixedPayoutRecipientRepository
{
    Task<FixedPayoutRecipient?> FindByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<FixedPayoutRecipient>> ListAsync(string? ownerAdminExternalId, CancellationToken cancellationToken = default);
    Task AddAsync(FixedPayoutRecipient entity, CancellationToken cancellationToken = default);
    void Update(FixedPayoutRecipient entity);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}
