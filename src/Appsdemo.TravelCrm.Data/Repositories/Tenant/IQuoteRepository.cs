using Appsdemo.TravelCrm.Core.Common;
using Appsdemo.TravelCrm.Core.Models.Tenant;

namespace Appsdemo.TravelCrm.Data.Repositories.Tenant;

public interface IQuoteRepository
{
    Task<PagedResult<QuoteListItem>> ListAsync(QuoteFilter filter);
    Task<Quote?> GetByIdAsync(Guid id);
    Task<Guid> InsertAsync(Quote quote);
    Task UpdateAsync(Quote quote);
    Task SoftDeleteAsync(Guid id, Guid? deletedBy);
    Task UpdateStatusAsync(Guid id, string status, Guid? updatedBy);
}

public sealed class QuoteFilter
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public Guid? LeadId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
