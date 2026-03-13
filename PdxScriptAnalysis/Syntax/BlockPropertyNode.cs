using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Syntax
{
    /// <summary>
    /// 右辺がブロックであるプロパティノード。
    /// </summary>
    /// <param name="Key">プロパティの名前を表すトークン</param>
    /// <param name="Operator">プロパティの演算子を表すトークン</param>
    /// <param name="Value">プロパティの値を表すブロックノード</param>
    /// <param name="Span">ソーステキスト上の範囲を表すスパン</param>
    public sealed record BlockPropertyNode(
        SyntaxToken Key,
        SyntaxToken Operator,
        BlockNode Value,
        TextSpan Span)
        : PropertyNode(Key, Operator, Span)
    {
        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
            => visitor.VisitBlockProperty(this);
        public override void Accept(SyntaxWalker walker)
            => walker.VisitBlockProperty(this);
        public override IEnumerable<SyntaxNode> ChildNodes()
            => [Value];

        public override string ToString()
            => $"{GetType().Name}: {Key.Text} {Operator.Text} {{ {Value.Children.Count} children }} at {Span}";
    }
}