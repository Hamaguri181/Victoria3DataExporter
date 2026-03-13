using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Syntax
{
    /// <summary>
    /// 右辺が修飾子付きブロックであるプロパティノード。
    /// 例えば、color = hsv {10 20 30 } のようなプロパティを表す。
    /// </summary>
    /// <param name="Key">プロパティの名前を表すトークン</param>
    /// <param name="Operator">プロパティの演算子を表すトークン</param>
    /// <param name="TypeQualifier">プロパティの型修飾子を表すトークン</param>
    /// <param name="Value">プロパティの値を表すブロックノード</param>
    /// <param name="Span">ソーステキスト上の範囲を表すスパン</param>
    public sealed record TypedBlockPropertyNode(
        SyntaxToken Key,
        SyntaxToken Operator,
        SyntaxToken TypeQualifier,
        BlockNode Value,
        TextSpan Span)
        : PropertyNode(Key, Operator, Span)
    {
        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
            => visitor.VisitTypedBlockProperty(this);
        public override void Accept(SyntaxWalker walker)
            => walker.VisitTypedBlockProperty(this);
        public override IEnumerable<SyntaxNode> ChildNodes()
            => [Value];

        public override string ToString()
            => $"{GetType().Name}: {Key.Text} {Operator.Text} {TypeQualifier.Text} {{ {Value.Children.Count} children }} at {Span}";
    }
}
