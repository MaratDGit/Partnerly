namespace Partnerly.Helpers
{
    public static class ExternalLoginOptions
    {
        public const string AppleScheme = "Apple";

        public static bool IsAppleConfigured(IConfiguration config)
        {
            return !string.IsNullOrWhiteSpace(
                config["Authentication:Apple:ClientId"]);
        }
    }
}
