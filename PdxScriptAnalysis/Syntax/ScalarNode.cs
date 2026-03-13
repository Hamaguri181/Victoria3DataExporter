using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Syntax
{
    /// <summary>
    /// トークン1つを表す構文ノード。
    /// </summary>
    /// <param name="Token">スカラー値を表すトークン</param>
    /// <param name="Span">ソーステキスト上の範囲を表すスパン</param>
    public sealed record ScalarNode(
        SyntaxToken Token,
        TextSpan Span)
        : SyntaxNode(Span)
    {
        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
            => visitor.VisitScalar(this);
        public override void Accept(SyntaxWalker walker)
            => walker.VisitScalar(this);
        public override IEnumerable<SyntaxNode> ChildNodes()
            => [];

        public override string ToString()
            => $"{GetType().Name}: {Token.Text} at {Span}";
    }
}
