namespace Partnerly.Integrations.Common
{
    // Thrown by service clients with a message that is safe to show in run
    // history (never contains tokens).
    public class IntegrationException : Exception
    {
        public IntegrationException(string message, bool retryable = true)
            : base(message)
        {
            Retryable = retryable;
        }

        public bool Retryable { get; }
    }
}
