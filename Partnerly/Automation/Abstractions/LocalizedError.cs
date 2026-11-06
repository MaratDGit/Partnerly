using System.Text.RegularExpressions;

namespace Partnerly.Automation.Abstractions
{
    // A step runs in the background, where nobody has a language. Its error therefore carries the
    // key of the translated text next to the English one; the page that shows it picks the language
    // of the person who looks (Resolve), everything else (e-mails, logs) shows the English text (Strip).
    public static class LocalizedError
    {
        private const char Open = (char)0x27E6;
        private const char Close = (char)0x27E7;

        private static readonly Regex Marker = new(
            Open + "loc:(?<key>[A-Za-z0-9_]+)[|](?<text>.*?)" + Close,
            RegexOptions.Compiled | RegexOptions.Singleline);

        public static string Mark(string key, string english) => Open + "loc:" + key + "|" + english + Close;

        public static string? Resolve(string? text, Func<string, string?> lookup) =>
            text == null || !text.Contains(Open)
                ? text
                : Marker.Replace(text, m => lookup(m.Groups["key"].Value) is { Length: > 0 } found ? found : m.Groups["text"].Value);

        public static string? Strip(string? text) => Resolve(text, _ => null);
    }
}
