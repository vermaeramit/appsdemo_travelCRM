using Appsdemo.TravelCrm.Core.Common;
using Appsdemo.TravelCrm.Core.Models.Tenant;

namespace Appsdemo.TravelCrm.Data.Repositories.Tenant;

public interface ILeadRepository
{
    Task<PagedResult<LeadListItem>> ListAsync(LeadFilter filter);
    Task<Lead?> GetByIdAsync(Guid id);
    Task<Guid> InsertAsync(Lead lead);
    Task UpdateAsync(Lead lead);
    Task SoftDeleteAsync(Guid id, Guid? deletedBy);
    Task<IReadOnlyList<LeadFollowup>> ListFollowupsAsync(Guid leadId);
    Task<Guid> InsertFollowupAsync(LeadFollowup followup);
}

public sealed class LeadFilter
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public Guid? AssignedTo { get; set; }
    public Guid? BranchId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
