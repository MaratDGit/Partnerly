using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;

namespace Partnerly.Controllers;

public class LanguageController : Controller
{
    private static readonly string[] SupportedCultures =
    {
        "en",
        "ru",
        "hy"
    };

    private readonly ISystemSettingsService _systemSettingsService;

    public LanguageController(ISystemSettingsService systemSettingsService)
    {
        _systemSettingsService = systemSettingsService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLanguage(string culture, string returnUrl)
    {
        if (!SupportedCultures.Contains(culture))
        {
            var settings = await _systemSettingsService.GetCurrentAsync();
            culture = settings?.DefaultLanguage is "en" or "ru" or "hy"
                ? settings.DefaultLanguage
                : SystemSettingsDefaults.DefaultLanguage;
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(
                new RequestCulture(culture)),
            new CookieOptions
            {
                Path = "/",
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        if (WantsJson(Request))
        {
            return Json(new { ok = true, culture });
        }

        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            returnUrl = "/";
        }

        return LocalRedirect(returnUrl);
    }

    private static bool WantsJson(HttpRequest request)
    {
        if (string.Equals(
            request.Headers["X-Requested-With"],
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var accept = request.Headers["Accept"].ToString();
        return accept.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }
}