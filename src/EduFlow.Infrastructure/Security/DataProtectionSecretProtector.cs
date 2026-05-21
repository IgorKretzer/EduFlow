using EduFlow.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace EduFlow.Infrastructure.Security;

public sealed class DataProtectionSecretProtector : ISecretProtector
{
    private const string Prefix = "dp1:";
    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("EduFlow.ErpCredentials.v1");
    }

    public string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;
        if (plainText.StartsWith(Prefix, StringComparison.Ordinal)) return plainText;
        return Prefix + _protector.Protect(plainText);
    }

    public string Unprotect(string storedValue)
    {
        if (string.IsNullOrEmpty(storedValue)) return storedValue;
        if (!storedValue.StartsWith(Prefix, StringComparison.Ordinal))
            return storedValue;

        return _protector.Unprotect(storedValue[Prefix.Length..]);
    }
}
