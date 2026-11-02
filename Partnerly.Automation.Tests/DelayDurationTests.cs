using System.Text.Json;
using Partnerly.Automation.Engine;

namespace Partnerly.Automation.Tests
{
    public class DelayDurationTests
    {
        [Theory]
        [InlineData("5", "minutes", 300)]
        [InlineData("2", "hours", 7200)]
        [InlineData("1", "days", 86400)]
        [InlineData("12", "seconds", 12)]
        public void Amount_and_unit_convert_to_seconds(string amount, string unit, int seconds)
        {
            var config = JsonDocument.Parse($@"{{ ""amount"": {amount}, ""unit"": ""{unit}"" }}").RootElement;
            Assert.True(DelayDuration.TryRead(config, out var delay));
            Assert.Equal(TimeSpan.FromSeconds(seconds), delay);
        }

        [Fact]
        public void Legacy_timespan_still_parses()
        {
            var config = JsonDocument.Parse(@"{ ""delay"": ""00:00:05"" }").RootElement;
            Assert.True(DelayDuration.TryRead(config, out var delay));
            Assert.Equal(TimeSpan.FromSeconds(5), delay);
        }

        [Fact]
        public void Legacy_integer_seconds_still_parse()
        {
            var config = JsonDocument.Parse(@"{ ""startAfter"": ""90"" }").RootElement;
            Assert.True(DelayDuration.TryRead(config, out var delay));
            Assert.Equal(TimeSpan.FromSeconds(90), delay);
        }
    }
}
