using FluentAssertions;
using PdxScriptAnalysis;
using PdxScriptAnalysis.Syntax;
using Victoria3.GameData;
using Victoria3.Loading.Loaders;

namespace Victoria3.Loading.Tests.Loaders
{
    public class PropertyNodeParsersTests
    {
        private enum TestEnum
        {
            GrandPrincipality,
        }

        private static PropertyNode ParseSinglePropertyNode(string text)
        {
            var root = ScriptTree.ParseText(text).Root;
            root.Children.Should().ContainSingle();
            return root.Children[0].Should().BeAssignableTo<PropertyNode>().Subject;
        }

        [Fact(DisplayName = "TryParseToString: ScalarPropertyNode を文字列として解析できる")]
        public void TryParseToString_WithScalarProperty_ReturnsTrue()
        {
            var node = ParseSinglePropertyNode("name = test_country");

            var ok = PropertyNodeParsers.TryParseToString(node, out var value, out var diagnostic);

            ok.Should().BeTrue();
            value.Should().Be("test_country");
            diagnostic.Should().BeNull();
        }

        [Fact(DisplayName = "TryParseToString: BlockPropertyNode を渡すと失敗する")]
        public void TryParseToString_WithBlockProperty_ReturnsFalse()
        {
            var node = ParseSinglePropertyNode("name = { test_country }");

            var ok = PropertyNodeParsers.TryParseToString(node, out var value, out var diagnostic);

            ok.Should().BeFalse();
            value.Should().BeNull();
            diagnostic.Should().NotBeNull();
            diagnostic.Message.Should().Contain("Expected a scalar property node");
        }

        [Fact(DisplayName = "TryParseToStringList: 文字列リストを解析できる")]
        public void TryParseToStringList_WithScalarChildren_ReturnsTrue()
        {
            var node = ParseSinglePropertyNode("cultures = { north_german south_german }");

            var ok = PropertyNodeParsers.TryParseToStringList(node, out var values, out var diagnostic);

            ok.Should().BeTrue();
            values.Should().Equal("north_german", "south_german");
            diagnostic.Should().BeNull();
        }

        [Fact(DisplayName = "TryParseToStringList: 子要素にスカラー以外があると失敗する")]
        public void TryParseToStringList_WithNonScalarChild_ReturnsFalse()
        {
            var node = ParseSinglePropertyNode("cultures = { north_german { foo = bar } }");

            var ok = PropertyNodeParsers.TryParseToStringList(node, out var values, out var diagnostic);

            ok.Should().BeFalse();
            values.Should().BeNull();
            diagnostic.Should().NotBeNull();
            diagnostic.Message.Should().Contain("Expected all children of the block");
        }

        [Theory(DisplayName = "TryParseToBool: yes/no を真偽値に変換できる")]
        [InlineData("yes", true)]
        [InlineData("no", false)]
        public void TryParseToBool_ValidValue_ReturnsTrue(string raw, bool expected)
        {
            var node = ParseSinglePropertyNode($"flag = {raw}");

            var ok = PropertyNodeParsers.TryParseToBool(node, out var value, out var diagnostic);

            ok.Should().BeTrue();
            value.Should().Be(expected);
            diagnostic.Should().BeNull();
        }

        [Fact(DisplayName = "TryParseToBool: yes/no 以外は失敗する")]
        public void TryParseToBool_InvalidValue_ReturnsFalse()
        {
            var node = ParseSinglePropertyNode("flag = true");

            var ok = PropertyNodeParsers.TryParseToBool(node, out _, out var diagnostic);

            ok.Should().BeFalse();
            diagnostic.Should().NotBeNull();
            diagnostic.Message.Should().Contain("yes").And.Contain("no");
        }

        [Fact(DisplayName = "TryParseToInt: 整数を解析できる")]
        public void TryParseToInt_ValidValue_ReturnsTrue()
        {
            var node = ParseSinglePropertyNode("rank = 42");

            var ok = PropertyNodeParsers.TryParseToInt(node, out var value, out var diagnostic);

            ok.Should().BeTrue();
            value.Should().Be(42);
            diagnostic.Should().BeNull();
        }

        [Fact(DisplayName = "TryParseToDecimal: 小数を解析できる")]
        public void TryParseToDecimal_ValidValue_ReturnsTrue()
        {
            var node = ParseSinglePropertyNode("ratio = 12.5");

            var ok = PropertyNodeParsers.TryParseToDecimal(node, out var value, out var diagnostic);

            ok.Should().BeTrue();
            value.Should().Be(12.5m);
            diagnostic.Should().BeNull();
        }

        [Theory(DisplayName = "TryParseToEnum: '_' '-' を正規化して列挙値に変換できる")]
        [InlineData("grand_principality")]
        [InlineData("grand-principality")]
        public void TryParseToEnum_NormalizedText_ReturnsTrue(string raw)
        {
            var node = ParseSinglePropertyNode($"tier = {raw}");

            var ok = PropertyNodeParsers.TryParseToEnum<TestEnum>(node, out var value, out var diagnostic);

            ok.Should().BeTrue();
            value.Should().Be(TestEnum.GrandPrincipality);
            diagnostic.Should().BeNull();
        }

        [Fact(DisplayName = "TryParseToGameColor: RGB ブロックを解析できる")]
        public void TryParseToGameColor_RgbBlock_ReturnsTrue()
        {
            var node = ParseSinglePropertyNode("color = { 147 130 110 }");

            var ok = PropertyNodeParsers.TryParseToGameColor(node, out var color, out var diagnostic);

            ok.Should().BeTrue();
            color.Should().Be(new GameColor(147, 130, 110));
            diagnostic.Should().BeNull();
        }

        [Fact(DisplayName = "TryParseToGameColor: HSV 型ブロックを RGB に変換できる")]
        public void TryParseToGameColor_HsvTypedBlock_ReturnsTrue()
        {
            var node = ParseSinglePropertyNode("color = hsv { 0.0 0.0 1.0 }");

            var ok = PropertyNodeParsers.TryParseToGameColor(node, out var color, out var diagnostic);

            ok.Should().BeTrue();
            color.Should().Be(new GameColor(255, 255, 255));
            diagnostic.Should().BeNull();
        }

        [Fact(DisplayName = "TryParseToGameColor: 不正な type qualifier は失敗する")]
        public void TryParseToGameColor_InvalidTypeQualifier_ReturnsFalse()
        {
            var node = ParseSinglePropertyNode("color = cmyk { 0 0 0 }");

            var ok = PropertyNodeParsers.TryParseToGameColor(node, out _, out var diagnostic);

            ok.Should().BeFalse();
            diagnostic.Should().NotBeNull();
            diagnostic.Message.Should().Contain("Invalid type qualifier");
        }
    }
}
