using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Syntax
{
    /// <summary>
    /// 全ての構文ノードの基底クラス。構文ノードは、ソーステキスト上の位置範囲を表す<see cref="TextSpan"/>を持ち、構文ツリー内の子ノードを列挙できる。
    /// </summary>
    /// <param name="Span">ソーステキスト上の位置範囲を表す<see cref="TextSpan"/>。</param>
    public abstract record SyntaxNode(TextSpan Span)
    {
        /// <summary>
        /// Visitorのエントリーポイント。構文ノードの種類に応じて、適切なVisitメソッドが呼び出される。
        /// </summary>
        /// <typeparam name="TResult">Visitorが返す結果の型。</typeparam>
        /// <param name="visitor">訪問するVisitor。</param>
        /// <returns>Visitorが返す結果。Visitorが結果を返さない場合は<see langword="default"/>。</returns>
        public abstract TResult Accept<TResult>(SyntaxVisitor<TResult> visitor);

        /// <summary>
        /// Walkerのエントリーポイント。構文ノードの種類に応じて、適切なVisitメソッドが呼び出される。
        /// </summary>
        /// <param name="walker">訪問するWalker。</param>
        public abstract void Accept(SyntaxWalker walker);

        /// <summary>
        /// この構文ノードの子ノードを列挙する。子ノードは、構文ツリー内でこのノードの下に位置するノードであり、直接的な親子関係にあるものを指す。
        /// </summary>
        /// <returns>この構文ノードの子ノードの列挙。</returns>
        public abstract IEnumerable<SyntaxNode> ChildNodes();

        /// <summary>
        /// この構文ノードの最初のトークンの行列位置を返す。
        /// </summary>
        public abstract LinePosition LinePosition { get; }

        public override string ToString()
            => $"{GetType().Name} at {Span}";
    }
}
