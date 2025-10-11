using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

public static class AvatarGenerator
{
    //private static readonly Color[] BackgroundColors = new[]
    //{
    //    Color.Parse("#F44336"), // Красный
    //    Color.Parse("#2196F3"), // Синий
    //    Color.Parse("#4CAF50"), // Зелёный
    //    Color.Parse("#FF9800"), // Оранжевый
    //    Color.Parse("#9C27B0"), // Фиолетовый
    //    Color.Parse("#3F51B5"), // Индиго
    //    Color.Parse("#009688"),  // Бирюзовый
    //    Color.Parse("#5717e3")
    //};

    public static byte[] GenerateAvatar(string firstName, string lastName, int size = 200)
    {
        string initials = $"{(string.IsNullOrWhiteSpace(firstName) ? "?" : char.ToUpper(firstName[0]).ToString())}"
                        + $"{(string.IsNullOrWhiteSpace(lastName) ? "" : char.ToUpper(lastName[0]).ToString())}";

        var bgColor = Color.Parse("#696cff");
        using var image = new Image<Rgba32>(size, size, bgColor);

        var font = SystemFonts.CreateFont("Arial", size / 2.5f, FontStyle.Bold);

        var richOptions = new RichTextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Origin = new PointF(size / 2f, size / 2f)
        };

        image.Mutate(ctx =>
        {
            ctx.DrawText(richOptions, initials, Color.White);
        });

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }
}
