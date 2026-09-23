using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

public static class AvatarGenerator
{
    private static byte[] GenerateAvatar(string letter, int size = 200)
    {
        var bgColor = Color.Parse("#696cff");

        using var image = new Image<Rgba32>(size, size, bgColor);

        var font = SystemFonts.CreateFont(
            "Arial",
            size / 2.5f,
            FontStyle.Bold);

        var richOptions = new RichTextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Origin = new PointF(size / 2f, size / 2f)
        };

        image.Mutate(ctx =>
        {
            ctx.DrawText(
                richOptions,
                letter,
                Color.White);
        });

        using var ms = new MemoryStream();

        image.SaveAsPng(ms);

        return ms.ToArray();
    }

    public static string GetOrCreateAvatar(string firstName)
    {
        var letter = string.IsNullOrWhiteSpace(firstName)
            ? "?"
            : char.ToUpperInvariant(firstName.Trim()[0]).ToString();

        var directory = Path.Combine(
            "wwwroot",
            "assets",
            "img",
            "profiles");

        Directory.CreateDirectory(directory);

        var fileName = $"{letter}.png";
        var filePath = Path.Combine(directory, fileName);

        if (!File.Exists(filePath))
        {
            var avatarBytes = GenerateAvatar(letter);

            File.WriteAllBytes(filePath, avatarBytes);
        }

        return filePath
            .Replace("wwwroot", "~")
            .Replace("\\", "/");
    }
}
