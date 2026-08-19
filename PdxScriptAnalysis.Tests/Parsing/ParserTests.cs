using PdxScriptAnalysis.Diagnostics;
using PdxScriptAnalysis.Syntax;

namespace PdxScriptAnalysis.Tests.Parsing
{
    public class ParserTests
    {
        private static RootNode Parse(string text)
            => ScriptTree.ParseText(text).Root;

        private static ScriptTree ParseToTree(string text)
            => ScriptTree.ParseText(text);

        private static T AssertSingleChild<T>(SyntaxNode node) where T : SyntaxNode
        {
            Assert.Single(node.ChildNodes());
            return Assert.IsType<T>(node.ChildNodes().First());
        }


        [Fact(DisplayName = "空入力は空のルートを生成する")]
        public void EmptyInput_ProducesEmptyRoot()
        {
            var root = Parse("");
            Assert.Empty(root.Children);
        }


        [Fact(DisplayName = "スカラープロパティの解析")]
        public void ScalarProperty_IsParsedCorrectly()
        {
            var root = Parse("key = value");
            var prop = AssertSingleChild<ScalarPropertyNode>(root);
            Assert.Equal("key", prop.Key.Text);
            Assert.Equal("=", prop.Operator.Text);
            var value = Assert.IsType<ScalarNode>(prop.Value);
            Assert.Equal("value", value.Token.Text);
        }

        [Fact(DisplayName = "文字列リテラルを持つスカラープロパティの解析")]
        public void ScalarProperty_WithStringLiteral_IsParsedCorrectly()
        {
            var root = Parse("key = \"string value\"");
            var prop = AssertSingleChild<ScalarPropertyNode>(root);
            Assert.Equal(SyntaxKind.StringLiteral, prop.Value.Token.Kind);
        }

        [Theory(DisplayName = "各種演算子を持つスカラープロパティの解析")]
        [InlineData("key = value", SyntaxKind.Equals)]
        [InlineData("key <= value", SyntaxKind.LessThanEquals)]
        [InlineData("key >= value", SyntaxKind.GreaterThanEquals)]
        [InlineData("key != value", SyntaxKind.NotEquals)]
        [InlineData("key ?= value", SyntaxKind.QuestionEquals)]
        [InlineData("key < value", SyntaxKind.LessThan)]
        [InlineData("key > value", SyntaxKind.GreaterThan)]
        public void ScalarProperty_WithVariousOperators_IsParsedCorrectly(string input, SyntaxKind expectedOperator)
        {
            var root = Parse(input);
            var prop = AssertSingleChild<ScalarPropertyNode>(root);
            Assert.Equal(expectedOperator, prop.Operator.Kind);
        }


        [Fact(DisplayName = "ブロックプロパティの解析")]
        public void BlockProperty_IsParsedCorrectly()
        {
            var root = Parse("block = { key = value foo = bar }");
            var prop = AssertSingleChild<BlockPropertyNode>(root);
            Assert.Equal("block", prop.Key.Text);
            Assert.Equal("=", prop.Operator.Text);
            var block = Assert.IsType<BlockNode>(prop.Value);
            Assert.Equal(2, block.Children.Count);
        }

        [Fact(DisplayName = "空のブロックプロパティの解析")]
        public void BlockProperty_EmptyBlock_IsParsedCorrectly()
        {
            var root = Parse("block = { }");
            var prop = AssertSingleChild<BlockPropertyNode>(root);
            var block = Assert.IsType<BlockNode>(prop.Value);
            Assert.Empty(block.Children);
        }

        [Fact(DisplayName = "ネストしたブロックプロパティの解析")]
        public void BlockProperty_NestedBlocks_IsParsedCorrectly()
        {
            var root = Parse("block = { innerBlock = { key = value } }");
            var prop = AssertSingleChild<BlockPropertyNode>(root);
            var innerProp = AssertSingleChild<BlockPropertyNode>(prop.Value);
            Assert.Equal("block", prop.Key.Text);
            Assert.Equal("innerBlock", innerProp.Key.Text);
            Assert.Single(innerProp.Value.Children);
        }


        [Fact(DisplayName = "修飾子付きブロックプロパティの解析")]
        public void TypedBlockProperty_IsParsedCorrectly()
        {
            var root = Parse("block = type { value1 value2 value3 }");
            var prop = AssertSingleChild<TypedBlockPropertyNode>(root);
            Assert.Equal("block", prop.Key.Text);
            Assert.Equal("=", prop.Operator.Text);
            Assert.Equal("type", prop.TypeQualifier.Text);
            Assert.Equal(3, prop.Value.Children.Count);
        }


        [Fact(DisplayName = "ブロックは単体のブロックをもつことができる")]
        public void Block_CanContainSingleBlock()
        {
            var root = Parse("block = { { key = value } }");
            var prop = AssertSingleChild<BlockPropertyNode>(root);
            var block = Assert.IsType<BlockNode>(prop.Value);
            Assert.Single(block.Children);
            var innerBlock = AssertSingleChild<BlockNode>(block);
            Assert.Single(innerBlock.Children);
        }


        [Fact(DisplayName = "閉じられていないブロックは診断を生成するが、ASTは生成される")]
        public void UnclosedBlock_ProducesDiagnosticAndAst()
        {
            var tree = ParseToTree("block = { key = value ");
            Assert.NotNull(tree.Root);

            var node = AssertSingleChild<BlockPropertyNode>(tree.Root);
            Assert.Equal("block", node.Key.Text);

            Assert.True(tree.HasErrorsOrWarnings);
            var diag = Assert.Single(tree.Diagnostics);
            Assert.Equal("Unexpected end of file. Expected '}' to close the block.", diag.Message);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal(22, diag.Span.Start);
            Assert.Equal(0, diag.Span.Length);
        }

        [Fact(DisplayName = "右辺が不正なプロパティは診断を生成するが、ASTは生成される")]
        public void MissingPropertyValue_ProducesDiagnosticAndAst()
        {
            var tree = ParseToTree("key = ");
            Assert.NotNull(tree.Root);
            var node = AssertSingleChild<ScalarPropertyNode>(tree.Root);
            Assert.Equal("key", node.Key.Text);
            Assert.Equal("=", node.Operator.Text);
            Assert.Equal(SyntaxKind.Unknown, node.Value.Token.Kind);
            Assert.True(tree.HasErrorsOrWarnings);
            var diag = Assert.Single(tree.Diagnostics);
            Assert.Equal("Invalid property value: \"\"", diag.Message);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal(6, diag.Span.Start);
            Assert.Equal(0, diag.Span.Length);
        }

        [Fact(DisplayName = "予期しないトークンは診断を生成するが、ASTは生成される")]
        public void UnexpectedToken_ProducesDiagnosticAndAst()
        {
            var tree = ParseToTree("}");
            Assert.NotNull(tree.Root);
            Assert.Empty(tree.Root.Children);
            Assert.True(tree.HasErrorsOrWarnings);
            var diag = Assert.Single(tree.Diagnostics);
            Assert.Equal("Unexpected token: \"}\"", diag.Message);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal(0, diag.Span.Start);
            Assert.Equal(1, diag.Span.Length);
        }

        [Fact(DisplayName = "複数のエラーが発生してもすべての診断が収集される")]
        public void MultipleErrors_AllDiagnosticsCollected()
        {
            var tree = ParseToTree("key = }\n block = { key2 = value2 ");
            Assert.NotNull(tree.Root);
            Assert.True(tree.HasErrorsOrWarnings);
            Assert.Equal(3, tree.Diagnostics.Count);

            var diag1 = tree.Diagnostics[0];
            Assert.Equal("Invalid property value: \"}\"", diag1.Message);
            Assert.Equal(DiagnosticSeverity.Error, diag1.Severity);
            Assert.Equal(6, diag1.Span.Start);
            Assert.Equal(1, diag1.Span.Length);

            var diag2 = tree.Diagnostics[1];
            Assert.Equal("Unexpected token: \"}\"", diag2.Message);
            Assert.Equal(DiagnosticSeverity.Error, diag2.Severity);
            Assert.Equal(6, diag2.Span.Start);
            Assert.Equal(1, diag2.Span.Length);

            var diag3 = tree.Diagnostics[2];
            Assert.Equal("Unexpected end of file. Expected '}' to close the block.", diag3.Message);
            Assert.Equal(DiagnosticSeverity.Error, diag3.Severity);
            Assert.Equal(33, diag3.Span.Start);
            Assert.Equal(0, diag3.Span.Length);
        }

        [Fact(DisplayName = "正しい構文の入力は診断を生成せず、正しいASTを生成する")]
        public void ValidInput_NoDiagnosticsAndCorrectAst()
        {
            var tree = ParseToTree("key = value\nblock = { innerKey = innerValue }");
            Assert.NotNull(tree.Root);
            Assert.False(tree.HasErrorsOrWarnings);
            Assert.Empty(tree.Diagnostics);
        }


        [Fact(DisplayName = "テキストスパンが正しく計算されている")]
        public void TextSpans_AreCalculatedCorrectly()
        {
            var root = Parse("key = value\nblock = { innerKey = innerValue }");
            Assert.Equal(0, root.Span.Start);
            Assert.Equal(45, root.Span.Length);
        }
    }
}