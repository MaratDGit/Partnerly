using Partnerly.Descriptors.Attributes.BaseAttributes;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace Partnerly.Helpers
{
    public static class PasswordGenerator
    {
        private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
        private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Digits = "0123456789";
        private const string Special = "!@#$%^&*(),.?\"{}|<>";

        public static string Generate(int length = 12)
        {
            if (length < 6 || length > 15)
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    "Password length must be between 6 and 15.");

            var password = new List<char>
            {
                GetRandomChar(Lowercase),
                GetRandomChar(Uppercase),
                GetRandomChar(Digits),
                GetRandomChar(Special)
            };

            var allCharacters =
                Lowercase +
                Uppercase +
                Digits +
                Special;

            while (password.Count < length)
            {
                password.Add(GetRandomChar(allCharacters));
            }

            Shuffle(password);

            var result = new string(password.ToArray());

            var attribute = new PasswordValidationAttribute(
                min: 6,
                max: 15);

            var validationResult = attribute.GetValidationResult(
                result,
                new ValidationContext(result));

            if (validationResult != ValidationResult.Success)
            {
                return Generate(length);
            }

            return result;
        }

        private static char GetRandomChar(string characters)
        {
            var index = RandomNumberGenerator.GetInt32(
                characters.Length);

            return characters[index];
        }

        private static void Shuffle(List<char> characters)
        {
            for (var i = characters.Count - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);

                (characters[i], characters[j]) =
                    (characters[j], characters[i]);
            }
        }
    }
}
