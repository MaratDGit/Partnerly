using System.Collections.ObjectModel;

namespace Partnerly.Helpers
{
    public static class TimeZoneHelper
    {
        public static readonly ReadOnlyCollection<(string Id, string Label)> Choices =
            Array.AsReadOnly(new (string Id, string Label)[]
            {
                ("UTC", "UTC"),
                ("Asia/Yerevan", "Asia/Yerevan (UTC+4)"),
                ("Europe/Moscow", "Europe/Moscow"),
                ("Europe/Berlin", "Europe/Berlin"),
                ("Europe/London", "Europe/London"),
                ("America/New_York", "America/New_York"),
                ("America/Los_Angeles", "America/Los_Angeles")
            });

        private static readonly Dictionary<string, string> IanaToWindows =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["UTC"] = "UTC",
                ["Etc/UTC"] = "UTC",
                ["Asia/Yerevan"] = "Caucasus Standard Time",
                ["Europe/Moscow"] = "Russian Standard Time",
                ["Europe/Berlin"] = "W. Europe Standard Time",
                ["Europe/London"] = "GMT Standard Time",
                ["America/New_York"] = "Eastern Standard Time",
                ["America/Los_Angeles"] = "Pacific Standard Time"
            };

        public static TimeZoneInfo Resolve(string? timeZoneId)
        {
            var id = string.IsNullOrWhiteSpace(timeZoneId)
                ? SystemSettingsDefaults.TimeZoneId
                : timeZoneId.Trim();

            if (TryFind(id, out var zone))
                return zone;

            if (IanaToWindows.TryGetValue(id, out var windowsId) && TryFind(windowsId, out zone))
                return zone;

            return TimeZoneInfo.Utc;
        }

        public static DateTime ToLocal(DateTime value, TimeZoneInfo timeZone)
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };

            return TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
        }

        public static DateTime StartOfCalendarDayUtc(DateTime calendarDate, TimeZoneInfo timeZone)
        {
            var localMidnight = DateTime.SpecifyKind(calendarDate.Date, DateTimeKind.Unspecified);

            if (timeZone.IsInvalidTime(localMidnight))
                localMidnight = localMidnight.AddHours(1);

            return TimeZoneInfo.ConvertTimeToUtc(localMidnight, timeZone);
        }

        public static DateTime ExclusiveEndOfCalendarDayUtc(DateTime calendarDate, TimeZoneInfo timeZone) =>
            StartOfCalendarDayUtc(calendarDate.Date.AddDays(1), timeZone);

        public static string Format(DateTime value, TimeZoneInfo timeZone, string format) =>
            ToLocal(value, timeZone).ToString(format);

        private static bool TryFind(string id, out TimeZoneInfo zone)
        {
            try
            {
                zone = TimeZoneInfo.FindSystemTimeZoneById(id);
                return true;
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }

            zone = TimeZoneInfo.Utc;
            return false;
        }
    }

    public static class SystemSettingsDefaults
    {
        public const string ApplicationName = "Belote";
        public const string TimeZoneId = "Asia/Yerevan";
        public const string DefaultLanguage = "en";
        public const int PersistentCookieDays = 14;
        public const int SessionExpireMinutes = 60;
        public const int MaxProfilePhotoSizeMb = 5;
        public const int MaxAttachmentSizeMb = 10;
        public const string AllowedAttachmentExtensions = "jpg,jpeg,png,gif,webp,pdf,doc,docx,xls,xlsx,txt,zip";
        public const int MaxFailedLoginAttempts = 0;
        public const int LockoutDurationMinutes = 15;
    }
}
