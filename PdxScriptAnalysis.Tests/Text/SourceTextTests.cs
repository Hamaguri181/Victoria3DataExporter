using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Tests.Text
{
    public class SourceTextTests
    {
        [Fact(DisplayName = "1行目のLinePositionのLineの値は0であること")]
        public void GetLinePosition_FirstLine_ReturnsZero()
        {
            var source = SourceText.From("line1\nline2");
            var linePosition = source.GetLinePosition(0);
            Assert.Equal(0, linePosition.Line);
        }

        [Fact(DisplayName = "改行の後のLinePositionのLineの値は1であること")]
        public void GetLinePosition_AfterNewLine_ReturnsOne()
        {
            var source = SourceText.From("line1\nline2");
            var linePosition = source.GetLinePosition(6);
            Assert.Equal(1, linePosition.Line);
        }

        [Fact(DisplayName = "行の先頭のLinePositionのCharacterの値は0であること")]
        public void GetLinePosition_StartOfLine_ReturnsZero()
        {
            var source = SourceText.From("line1\nline2");
            var linePosition = source.GetLinePosition(0);
            Assert.Equal(0, linePosition.Character);
        }

        [Fact(DisplayName = "CRLFは合わせて改行として扱われること")]
        public void GetLinePosition_CRLF_ReturnsCorrectLine()
        {
            var source = SourceText.From("line1\r\nline2");
            var linePosition = source.GetLinePosition(7);
            Assert.Equal(1, linePosition.Line);
        }


        [Fact(DisplayName = "GetSubTextは指定したTextSpanに対応する部分文字列を返すこと")]
        public void GetSubText_ValidSpan_ReturnsSubstring()
        {
            var source = SourceText.From("Hello, World!");
            var span = new TextSpan(7, 5);
            var subText = source.GetSubText(span);
            Assert.Equal("World", subText);
        }

        [Fact(DisplayName = "GetSubTextは空のTextSpanに対して空文字列を返すこと")]
        public void GetSubText_EmptySpan_ReturnsEmptyString()
        {
            var source = SourceText.From("Hello, World!");
            var span = new TextSpan(5, 0);
            var subText = source.GetSubText(span);
            Assert.Equal(string.Empty, subText);
        }


        [Fact(DisplayName = "Fromはnullを受け入れないこと")]
        public void From_NullInput_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => SourceText.From(null!));
        }

        [Fact(DisplayName = "GetLinePositionは位置が範囲外の場合にArgumentOutOfRangeExceptionをスローすること")]
        public void GetLinePosition_OutOfRange_ThrowsArgumentOutOfRangeException()
        {
            var source = SourceText.From("line1\nline2");
            Assert.Throws<ArgumentOutOfRangeException>(() => source.GetLinePosition(100));
        }

        [Fact(DisplayName = "GetSubTextはTextSpanが範囲外の場合にArgumentOutOfRangeExceptionをスローすること")]
        public void GetSubText_OutOfRange_ThrowsArgumentOutOfRangeException()
        {
            var source = SourceText.From("Hello, World!");
            var span = new TextSpan(0, 100);
            Assert.Throws<ArgumentOutOfRangeException>(() => source.GetSubText(span));
        }

        [Fact(DisplayName = "GetPositionは行・列位置が範囲外の場合にArgumentOutOfRangeExceptionをスローすること")]
        public void GetPosition_OutOfRange_ThrowsArgumentOutOfRangeException()
        {
            var source = SourceText.From("line1");
            var linePosition = new LinePosition(0, 5);
            Assert.Throws<ArgumentOutOfRangeException>(() => source.GetPosition(linePosition));
        }
    }
}