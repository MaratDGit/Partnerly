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
                new RequestCulture(culture)));

        return LocalRedirect(returnUrl);
    }
}