namespace Partnerly.Helpers
{
    public static class ReferralCodeGenerator
    {
        private static Random _random = new Random();

        public static string GenerateCode(string? usernameOrEmail, int randomLength = 4)
        {
            if (usernameOrEmail == null)
                usernameOrEmail = "USR1";

            string basePart = new string(usernameOrEmail
                .Where(char.IsLetterOrDigit)
                .Take(4) 
                .ToArray())
                .ToUpper();

            if (string.IsNullOrWhiteSpace(basePart))
                basePart = "USR"; 

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string randomPart = new string(Enumerable.Repeat(chars, randomLength)
                .Select(s => s[_random.Next(s.Length)]).ToArray());

            return basePart + randomPart;
        }
    }
}
