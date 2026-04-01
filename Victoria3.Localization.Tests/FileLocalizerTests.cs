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

        [Fact(DisplayName = "キーがnullの場合、Localizeは空文字列を返す")]
        public void Localize_NullKey_ReturnsEmptyString()
        {
            var localizer = FileLocalizer.FromText("""
                known:0 "known-value"
                """);

            localizer.Localize(null).Should().Be(string.Empty);
        }

        [Fact(DisplayName = "キーがnullの場合、TryLocalizeはfalseを返す")]
        public void TryLocalize_NullKey_ReturnsFalse()
        {
            var localizer = FileLocalizer.FromText("""
                known:0 "known-value"
                """);
            localizer.TryLocalize(null, out var _).Should().BeFalse();
        }

        [Fact(DisplayName = "キーに接頭辞がある場合、接頭辞を削除してローカライズされる")]
        public void Localize_KeyWithPrefix_PrefixRemoved()
        {
            var localizer = FileLocalizer.FromText("""
                known:0 "known-value"
                """);
            localizer.Localize("prefix:known").Should().Be("known-value");
            localizer.TryLocalize("prefix:known", out var value).Should().BeTrue();
            value.Should().Be("known-value");
        }
    }
}