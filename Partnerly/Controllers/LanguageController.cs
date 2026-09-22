using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Partnerly.Controllers;

public class LanguageController : Controller
{
    private static readonly string[] SupportedCultures =
    {
        "en",
        "ru",
        "hy"
    };

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        if (!SupportedCultures.Contains(culture))
        {
            culture = "en";
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(
                new RequestCulture(culture)));

        return LocalRedirect(returnUrl);
    }
}