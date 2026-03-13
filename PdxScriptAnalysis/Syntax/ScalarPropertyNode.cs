using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Syntax
{
    /// <summary>
    /// 右辺がスカラー値であるプロパティノード。
    /// </summary>
    /// <param name="Key">プロパティの名前を表すトークン</param>
    /// <param name="Operator">プロパティの演算子を表すトークン</param>
    /// <param name="Value">プロパティの値を表すスカラー値ノード</param>
    /// <param name="Span">ソーステキスト上の範囲を表すスパン</param>
    public sealed record ScalarPropertyNode(
        SyntaxToken Key,
        SyntaxToken Operator,
        ScalarNode Value,
        TextSpan Span)
        : PropertyNode(Key, Operator, Span)
    {
        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
            => visitor.VisitScalarProperty(this);
        public override void Accept(SyntaxWalker walker)
            => walker.VisitScalarProperty(this);
        public override IEnumerable<SyntaxNode> ChildNodes()
            => [Value];

        public override string ToString()
            => $"{GetType().Name}: {Key.Text} {Operator.Text} {Value.Token.Text} at {Span}";
    }
}
