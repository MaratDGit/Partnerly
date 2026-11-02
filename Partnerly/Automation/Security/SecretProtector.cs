using Microsoft.AspNetCore.DataProtection;
using Partnerly.Automation.Abstractions;

namespace Partnerly.Automation.Security
{
    public class SecretProtector : ISecretProtector
    {
        private readonly IDataProtector _protector;

        public SecretProtector(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("Automation.Connection");
        }

        public string Protect(string plaintext) => _protector.Protect(plaintext ?? string.Empty);

        public string Unprotect(string protectedValue) => _protector.Unprotect(protectedValue ?? string.Empty);
    }
}
