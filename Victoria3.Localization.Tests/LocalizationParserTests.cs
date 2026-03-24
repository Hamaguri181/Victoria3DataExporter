using FluentAssertions;
using Victoria3.Localization.Parsing;

namespace Victoria3.Localization.Tests
{
    public class LocalizationParserTests
    {
        [Fact(DisplayName = "基本形 key: \"value\" から辞書が作成される")]
        public void ParseText_BasicKeyValue_CreatesDictionary()
        {
            var text = """
            greeting: "hello"
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("greeting").WhoseValue.Should().Be("hello");
        }

        [Fact(DisplayName = "key:version \"value\" が正しく読み込まれる")]
        public void ParseText_KeyVersionValue_IsParsed()
        {
            var text = """
            greeting:0 "hello"
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("greeting").WhoseValue.Should().Be("hello");
        }

        [Fact(DisplayName = "ヘッダー行(l_japanese:)や空行がスキップされる")]
        public void ParseText_HeaderAndEmptyLines_AreSkipped()
        {
            var text = """
            l_japanese:

            greeting:0 "hello"

            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("greeting").WhoseValue.Should().Be("hello");
            result.Should().NotContainKey("l_japanese");
        }

        [Fact(DisplayName = "コメント行がスキップされる")]
        public void ParseText_CommentLines_AreSkipped()
        {
            var text = """
            # comment 1
            greeting:0 "hello"
            # comment 2
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("greeting").WhoseValue.Should().Be("hello");
        }

        [Fact(DisplayName = "value中の\\\\や\\\"が正しく認識される")]
        public void ParseText_EscapedCharacters_AreParsed()
        {
            var text = """
            path:0 "C:\\Program Files\\Victoria3"
            quote:0 "He said: \"hello\""
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(2);
            result.Should().ContainKey("path").WhoseValue.Should().Be(@"C:\Program Files\Victoria3");
            result.Should().ContainKey("quote").WhoseValue.Should().Be("He said: \"hello\"");
        }

        [Fact(DisplayName = "valueの後にコメントがあっても正しく認識される")]
        public void ParseText_TrailingComment_IsAllowed()
        {
            var text = """
            greeting:0 "hello" # trailing comment
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("greeting").WhoseValue.Should().Be("hello");
        }

        [Fact(DisplayName = "コロンがない場合スキップされる")]
        public void ParseText_NoColon_IsSkipped()
        {
            var text = """
            invalid line
            valid:0 "ok"
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("valid").WhoseValue.Should().Be("ok");
        }

        [Fact(DisplayName = "キーが空文字列のときスキップされる")]
        public void ParseText_EmptyKey_IsSkipped()
        {
            var text = """
            :0 "value"
            valid:0 "ok"
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("valid").WhoseValue.Should().Be("ok");
            result.Should().NotContainKey(string.Empty);
        }

        [Fact(DisplayName = "valueの開始引用符がないとスキップされる")]
        public void ParseText_MissingOpeningQuote_IsSkipped()
        {
            var text = """
            invalid:0 value
            valid:0 "ok"
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("valid").WhoseValue.Should().Be("ok");
            result.Should().NotContainKey("invalid");
        }

        [Fact(DisplayName = "終了引用符がないとスキップされる")]
        public void ParseText_MissingClosingQuote_IsSkipped()
        {
            var text = """
            invalid:0 "value
            valid:0 "ok"
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("valid").WhoseValue.Should().Be("ok");
            result.Should().NotContainKey("invalid");
        }

        [Fact(DisplayName = "valueの後にコメントでない文字列があるときスキップされる")]
        public void ParseText_TrailingNonComment_IsSkipped()
        {
            var text = """
            invalid:0 "value" trailing
            valid:0 "ok"
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("valid").WhoseValue.Should().Be("ok");
            result.Should().NotContainKey("invalid");
        }

        [Fact(DisplayName = "重複キーは後勝ちで上書きされる")]
        public void ParseText_DuplicateKeys_LastWins()
        {
            var text = """
            key:0 "first"
            key:0 "second"
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("key").WhoseValue.Should().Be("second");
        }
    }
}