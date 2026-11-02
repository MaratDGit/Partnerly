namespace Partnerly.Automation.Abstractions
{
    public interface ISecretProtector
    {
        string Protect(string plaintext);
        string Unprotect(string protectedValue);
    }
}
