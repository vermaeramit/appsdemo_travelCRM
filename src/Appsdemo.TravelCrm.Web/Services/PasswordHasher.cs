using Microsoft.AspNetCore.Identity;

namespace Appsdemo.TravelCrm.Web.Services;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hash, string password);
}

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _inner = new();
    private static readonly object Subject = new();

    public string Hash(string password) => _inner.HashPassword(Subject, password);

    public bool Verify(string hash, string password)
    {
        var result = _inner.VerifyHashedPassword(Subject, hash, password);
        return result == PasswordVerificationResult.Success
            || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
