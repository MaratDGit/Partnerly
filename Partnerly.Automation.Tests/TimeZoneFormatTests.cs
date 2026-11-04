using Partnerly.Helpers;

namespace Partnerly.Automation.Tests
{
    public class TimeZoneFormatTests
    {
        [Theory]
        [InlineData(DateTimeKind.Unspecified)]
        [InlineData(DateTimeKind.Utc)]
        public void Utc_value_is_shown_in_the_given_zone(DateTimeKind kind)
        {
            var stored = DateTime.SpecifyKind(new DateTime(2026, 11, 4, 10, 0, 0), kind);

            Assert.Equal("04.11.2026 14:00", TimeZoneHelper.FormatIn(stored, "Asia/Yerevan"));
            Assert.Equal("04.11.2026 10:00", TimeZoneHelper.FormatIn(stored, "UTC"));
        }

        [Fact]
        public void Missing_value_uses_placeholder_and_unknown_zone_falls_back_to_utc()
        {
            Assert.Equal("-", TimeZoneHelper.FormatIn(null, "UTC"));
            Assert.Equal("04.11.2026 10:00", TimeZoneHelper.FormatIn(new DateTime(2026, 11, 4, 10, 0, 0), "Not/AZone"));
        }
    }
}
