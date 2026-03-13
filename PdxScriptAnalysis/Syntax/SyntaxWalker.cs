namespace PdxScriptAnalysis.Syntax
{
    /// <summary>
    /// 構文ノードを巡回するためのクラス。
    /// 構文ノードの種類ごとに Visit メソッドが用意されており、必要に応じてオーバーライドして使用する。
    /// </summary>
    public abstract class SyntaxWalker
    {
        /// <summary>
        /// 指定された構文ノードを訪問する。ノードが null の場合は何もしない。
        /// </summary>
        /// <param name="node">訪問する構文ノード</param>
        public virtual void Visit(SyntaxNode node)
        {
            if (node is null) return;
            node.Accept(this);
        }

        /// <summary>
        /// デフォルトの訪問処理。ノードの種類に関係なく、子ノードをすべて訪問する。
        /// </summary>
        /// <param name="node">訪問する構文ノード</param>
        protected virtual void DefaultVisit(SyntaxNode node)
        {
            foreach (var child in node.ChildNodes())
            {
                Visit(child);
            }
        }

        /// <summary>
        /// スカラーノードの訪問処理。デフォルトでは DefaultVisit を呼び出す。
        /// </summary>
        /// <param name="node">訪問するスカラーノード</param>
        protected internal virtual void VisitScalar(ScalarNode node)
            => DefaultVisit(node);

        /// <summary>
        /// ブロックノードの訪問処理。デフォルトでは DefaultVisit を呼び出す。
        /// </summary>
        /// <param name="node">訪問するブロックノード</param>
        protected internal virtual void VisitBlock(BlockNode node)
            => DefaultVisit(node);

        /// <summary>
        /// プロパティノードの訪問処理。デフォルトでは DefaultVisit を呼び出す。
        /// </summary>
        /// <param name="node">訪問するプロパティノード</param>
        protected internal virtual void VisitProperty(PropertyNode node)
            => DefaultVisit(node);

        /// <summary>
        /// スカラープロパティノードの訪問処理。デフォルトではプロパティノードの訪問処理を呼び出す。
        /// </summary>
        /// <param name="node">訪問するスカラープロパティノード</param>
        protected internal virtual void VisitScalarProperty(ScalarPropertyNode node)
            => VisitProperty(node);

        /// <summary>
        /// ブロックプロパティノードの訪問処理。デフォルトではプロパティノードの訪問処理を呼び出す。
        /// </summary>
        /// <param name="node">訪問するブロックプロパティノード</param>
        protected internal virtual void VisitBlockProperty(BlockPropertyNode node)
            => VisitProperty(node);

        /// <summary>
        /// 型付きブロックプロパティノードの訪問処理。デフォルトではプロパティノードの訪問処理を呼び出す。
        /// </summary>
        /// <param name="node">訪問する型付きブロックプロパティノード</param>
        protected internal virtual void VisitTypedBlockProperty(TypedBlockPropertyNode node)
            => VisitProperty(node);

        /// <summary>
        /// ルートノードの訪問処理。デフォルトでは DefaultVisit を呼び出す。
        /// </summary>
        /// <param name="node">訪問するルートノード</param>
        protected internal virtual void VisitRoot(RootNode node)
            => DefaultVisit(node);
    }
}