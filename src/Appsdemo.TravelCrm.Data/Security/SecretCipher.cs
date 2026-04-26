using Microsoft.AspNetCore.DataProtection;

namespace Appsdemo.TravelCrm.Data.Security;

public interface ISecretCipher
{
    string Encrypt(string plain);
    string Decrypt(string cipher);
}

public sealed class SecretCipher : ISecretCipher
{
    private const string Purpose = "Appsdemo.TravelCrm.TenantDbSecret.v1";
    private readonly IDataProtector _protector;

    public SecretCipher(IDataProtectionProvider provider)
        => _protector = provider.CreateProtector(Purpose);

    public string Encrypt(string plain) => _protector.Protect(plain ?? "");
    public string Decrypt(string cipher) => _protector.Unprotect(cipher ?? "");
}
