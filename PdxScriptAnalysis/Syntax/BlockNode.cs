using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Syntax
{
    /// <summary>
    /// { ... } で囲まれた複数のノードを表す構文ノード。子ノードは、ブロック内のノードを順番に列挙する。
    /// </summary>
    /// <param name="Children">ブロック内の子ノードのリスト</param>
    /// <param name="Span">ソーステキスト上の範囲を表すスパン</param>
    public sealed record BlockNode(
        IReadOnlyList<SyntaxNode> Children,
        TextSpan Span)
        : SyntaxNode(Span)
    {
        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
            => visitor.VisitBlock(this);
        public override void Accept(SyntaxWalker walker)
            => walker.VisitBlock(this);
        public override IEnumerable<SyntaxNode> ChildNodes()
            => Children;
        public override LinePosition LinePosition
            => Children.Count > 0 ? Children[0].LinePosition : new LinePosition(0, Span.Length);

        public override string ToString()
            => $"{GetType().Name}: {Children.Count} children at {Span}";
    }
}