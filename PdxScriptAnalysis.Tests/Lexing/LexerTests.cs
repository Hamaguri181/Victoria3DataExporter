using PdxScriptAnalysis.Lexing;
using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Tests.Lexing
{
    public class LexerTests
    {
        private static IReadOnlyList<SyntaxToken> Tokenize(string text)
            => new Lexer(SourceText.From(text)).Tokenize();

        private static IReadOnlyList<SyntaxToken> TokenizeWithoutEOF(string text)
            => Tokenize(text).Where(t => !t.IsEndOfFile).ToList();

        [Theory(DisplayName = "必ず1文字である制御文字トークン")]
        [InlineData("{", SyntaxKind.LeftBrace)]
        [InlineData("}", SyntaxKind.RightBrace)]
        [InlineData("=", SyntaxKind.Equals)]
        public void SingleControlChar_IsRecognized(string input, SyntaxKind expected)
        {
            var tokens = TokenizeWithoutEOF(input);
            Assert.Single(tokens);
            Assert.Equal(expected, tokens[0].Kind);
            Assert.Equal(input, tokens[0].Text);
        }


        [Theory(DisplayName = "複合演算子の可能性があるトークン")]
        [InlineData("<=", SyntaxKind.LessThanEquals)]
        [InlineData(">=", SyntaxKind.GreaterThanEquals)]
        [InlineData("!=", SyntaxKind.NotEquals)]
        [InlineData("?=", SyntaxKind.QuestionEquals)]
        [InlineData("<", SyntaxKind.LessThan)]
        [InlineData(">", SyntaxKind.GreaterThan)]
        public void CompoundOperator_IsRecognized(string input, SyntaxKind expected)
        {
            var tokens = TokenizeWithoutEOF(input);
            Assert.Single(tokens);
            Assert.Equal(expected, tokens[0].Kind);
            Assert.Equal(input, tokens[0].Text);
        }

        [Theory(DisplayName = "不正な演算子")]
        [InlineData("!")]
        [InlineData("?")]
        public void InvalidOperator_IsUnknown(string input)
        {
            var tokens = TokenizeWithoutEOF(input);
            Assert.Single(tokens);
            Assert.Equal(SyntaxKind.Unknown, tokens[0].Kind);
            Assert.Equal(input, tokens[0].Text);
        }


        [Theory(DisplayName = "Atomトークン")]
        [InlineData("atom")]
        [InlineData("yes")]
        [InlineData("no")]
        [InlineData("c:JAP")]
        [InlineData("1842.1.t")]
        [InlineData("0.5")]
        [InlineData("-100")]
        public void Atom_IsRecognized(string input)
        {
            var tokens = TokenizeWithoutEOF(input);
            Assert.Single(tokens);
            Assert.Equal(SyntaxKind.Atom, tokens[0].Kind);
            Assert.Equal(input, tokens[0].Text);
        }

        [Fact(DisplayName = "Atomは制御文字で区切られる")]
        public void Atom_IsSeparatedByControlChars()
        {
            var input = "foo=bar{baz}";
            var tokens = TokenizeWithoutEOF(input);
            Assert.Equal(6, tokens.Count);
            Assert.Equal(SyntaxKind.Atom, tokens[0].Kind);
            Assert.Equal(SyntaxKind.Equals, tokens[1].Kind);
            Assert.Equal(SyntaxKind.Atom, tokens[2].Kind);
            Assert.Equal(SyntaxKind.LeftBrace, tokens[3].Kind);
            Assert.Equal(SyntaxKind.Atom, tokens[4].Kind);
            Assert.Equal(SyntaxKind.RightBrace, tokens[5].Kind);
            Assert.Equal("foo", tokens[0].Text);
            Assert.Equal("=", tokens[1].Text);
            Assert.Equal("bar", tokens[2].Text);
            Assert.Equal("{", tokens[3].Text);
            Assert.Equal("baz", tokens[4].Text);
            Assert.Equal("}", tokens[5].Text);
        }


        [Theory(DisplayName = "文字列リテラル")]
        [InlineData("\"Paradox\"")]
        [InlineData("\"String with spaces\"")]
        [InlineData("\"\"")] // 空文字列も有効
        public void StringLiteral_IsRecognized(string input)
        {
            var tokens = TokenizeWithoutEOF(input);
            Assert.Single(tokens);
            Assert.Equal(SyntaxKind.StringLiteral, tokens[0].Kind);
            Assert.Equal(input, tokens[0].Text);
        }

        [Fact(DisplayName = "文字列リテラルは閉じる必要がある")]
        public void StringLiteral_MustBeClosed()
        {
            var input = "\"Unclosed string";
            var tokens = TokenizeWithoutEOF(input);
            Assert.Single(tokens);
            Assert.Equal(SyntaxKind.Unknown, tokens[0].Kind);
            Assert.Equal(input, tokens[0].Text);
        }


        [Fact(DisplayName = "空白・改行は無視される")]
        public void Whitespace_IsIgnored()
        {
            var input = "  foo \t bar \n baz  ";
            var tokens = TokenizeWithoutEOF(input);
            Assert.Equal(3, tokens.Count);
            Assert.Equal("foo", tokens[0].Text);
            Assert.Equal("bar", tokens[1].Text);
            Assert.Equal("baz", tokens[2].Text);
        }

        [Fact(DisplayName = "コメントは行末まで無視される")]
        public void CommentLine_IsIgnored()
        {
            var input = "foo # this is a comment\nbar";
            var tokens = TokenizeWithoutEOF(input);
            Assert.Equal(2, tokens.Count);
            Assert.Equal("foo", tokens[0].Text);
            Assert.Equal("bar", tokens[1].Text);
        }


        [Fact(DisplayName = "空入力はEOFトークンのみ")]
        public void EmptyInput_ProducesOnlyEOF()
        {
            var input = "";
            var tokens = Tokenize(input);
            Assert.Single(tokens);
            Assert.Equal(SyntaxKind.EndOfFile, tokens[0].Kind);
        }

        [Fact(DisplayName = "最後のトークンはEOFである")]
        public void LastToken_IsEOF()
        {
            var input = "foo=bar";
            var tokens = Tokenize(input);
            Assert.True(tokens.Count > 0);
            Assert.Equal(SyntaxKind.EndOfFile, tokens[^1].Kind);
        }


        [Fact(DisplayName = "トークンスパンは正しく計算される")]
        public void TokenSpan_IsCalculatedCorrectly()
        {
            var input = "foo = bar";
            var tokens = Tokenize(input);
            Assert.Equal(4, tokens.Count);
            Assert.Equal(0, tokens[0].Span.Start);
            Assert.Equal(3, tokens[0].Span.Length);
            Assert.Equal(4, tokens[1].Span.Start);
            Assert.Equal(1, tokens[1].Span.Length);
            Assert.Equal(6, tokens[2].Span.Start);
            Assert.Equal(3, tokens[2].Span.Length);
            Assert.Equal(9, tokens[3].Span.Start);
            Assert.Equal(0, tokens[3].Span.Length);
        }
    }
}