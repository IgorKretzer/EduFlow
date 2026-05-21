namespace EduFlow.Application.Interfaces;

/// <summary>Protege segredos em repouso (ex.: token Sponte no banco).</summary>
public interface ISecretProtector
{
    string Protect(string plainText);
    string Unprotect(string storedValue);
}
