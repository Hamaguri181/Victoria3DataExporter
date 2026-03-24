using FluentAssertions;

namespace Victoria3.Localization.Tests
{
    public class FileLocalizerTests
    {
        [Fact(DisplayName = "Localize・TryLocalizeが想定通りの挙動を示す")]
        public void LocalizeAndTryLocalize_WorkAsExpected()
        {
            var localizer = FileLocalizer.FromText("""
                known:0 "known-value"
                """);

            localizer.Localize("known").Should().Be("known-value");
            localizer.Localize("unknown").Should().Be("unknown");

            localizer.TryLocalize("known", out var known).Should().BeTrue();
            known.Should().Be("known-value");

            localizer.TryLocalize("unknown", out var _).Should().BeFalse();
        }
    }
}
