using Appsdemo.TravelCrm.Data.Connection;
using Dapper;

namespace Appsdemo.TravelCrm.Data.Services;

public interface INumberSequenceService
{
    Task<string> NextAsync(string entity);
}

public sealed class NumberSequenceService : INumberSequenceService
{
    private readonly ITenantConnectionFactory _factory;
    public NumberSequenceService(ITenantConnectionFactory factory) => _factory = factory;

    public async Task<string> NextAsync(string entity)
    {
        using var conn = _factory.Open();
        var row = await conn.QuerySingleOrDefaultAsync<(string Prefix, long CurrentNo, int Padding)?>(@"
            UPDATE number_sequences
            SET current_no = current_no + 1
            WHERE entity = @entity
            RETURNING prefix, current_no, padding;",
            new { entity });

        if (row is null)
            throw new InvalidOperationException($"No number_sequence row for entity '{entity}'.");

        var (prefix, current, padding) = row.Value;
        return prefix + current.ToString(new string('0', Math.Max(1, padding)));
    }
}
