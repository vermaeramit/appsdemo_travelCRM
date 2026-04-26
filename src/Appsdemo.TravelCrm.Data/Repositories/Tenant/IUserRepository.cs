using Appsdemo.TravelCrm.Core.Models.Tenant;

namespace Appsdemo.TravelCrm.Data.Repositories.Tenant;

public interface IUserRepository
{
    Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername);
    Task<User?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<User>> ListAsync(string? search, int page, int pageSize);
    Task<int> CountAsync(string? search);
    Task<Guid> InsertAsync(User user);
    Task UpdateAsync(User user);
    Task UpdateLoginSuccessAsync(Guid id, string ip);
    Task UpdateLoginFailureAsync(Guid id, int newFailedCount, DateTimeOffset? lockUntil);
    Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId);
    Task<IReadOnlyList<Role>> GetRolesAsync(Guid userId);
}

public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> ListAsync();
    Task<Role?> GetByIdAsync(Guid id);
    Task<Guid> InsertAsync(Role role);
    Task SetPermissionsAsync(Guid roleId, IEnumerable<string> permissionKeys);
    Task<IReadOnlyList<string>> GetPermissionsAsync(Guid roleId);
}
