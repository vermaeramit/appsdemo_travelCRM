using Appsdemo.TravelCrm.Core.Models.Master;
using MasterTenant = Appsdemo.TravelCrm.Core.Models.Master.Tenant;

namespace Appsdemo.TravelCrm.Data.Repositories.Master;

public interface ITenantRepository
{
    Task<IReadOnlyList<MasterTenant>> ListAsync(string? search, int page, int pageSize);
    Task<int> CountAsync(string? search);
    Task<MasterTenant?> GetByIdAsync(Guid id);
    Task<MasterTenant?> GetByCodeAsync(string code);
    Task<Guid> InsertAsync(MasterTenant tenant);
    Task UpdateStatusAsync(Guid id, string status);
    Task<IReadOnlyDictionary<string, string>> GetFeaturesForPlanAsync(Guid planId);
}

public interface ISubscriptionPlanRepository
{
    Task<IReadOnlyList<SubscriptionPlan>> ListActiveAsync();
    Task<SubscriptionPlan?> GetByIdAsync(Guid id);
}

public interface IGlobalUserRepository
{
    Task<GlobalUser?> GetByEmailAsync(string email);
    Task UpdateLoginSuccessAsync(Guid id);
    Task UpdateLoginFailureAsync(Guid id, int newFailedCount, DateTimeOffset? lockUntil);
}
