using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Syntax
{
    /// <summary>
    /// ファイル全体を表す構文ノード。子ノードは、ファイル内のトップレベルノードを順番に列挙する。
    /// </summary>
    /// <param name="Children">ファイル内のトップレベルノードのリスト</param>
    /// <param name="Span">ソーステキスト上の範囲を表すスパン</param>
    public sealed record RootNode(
        IReadOnlyList<SyntaxNode> Children,
        TextSpan Span)
        : SyntaxNode(Span)
    {
        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
            => visitor.VisitRoot(this);
        public override void Accept(SyntaxWalker walker)
            => walker.VisitRoot(this);
        public override IEnumerable<SyntaxNode> ChildNodes()
            => Children;
        public override LinePosition LinePosition
            => Children.Count > 0 ? Children[0].LinePosition : new LinePosition(0, Span.Length);
    }
}
