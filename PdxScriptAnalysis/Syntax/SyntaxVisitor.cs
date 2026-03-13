namespace PdxScriptAnalysis.Syntax
{
    /// <summary>
    /// 構文ノードを訪問するためのビジターパターンの抽象クラス。
    /// 構文ノードの種類ごとに Visit メソッドが用意されており、必要に応じてオーバーライドして使用する。
    /// </summary>
    /// <typeparam name="TResult">訪問の結果の型</typeparam>
    public abstract class SyntaxVisitor<TResult>
    {
        /// <summary>
        /// 指定された構文ノードを訪問し、その結果を返す。
        /// </summary>
        /// <param name="node">訪問する構文ノード</param>
        /// <returns>訪問の結果</returns>
        public virtual TResult Visit(SyntaxNode node)
        {
            if (node is null) return default!;
            return node.Accept(this);
        }

        /// <summary>
        /// デフォルトの訪問処理を提供するメソッド。特定のノードタイプに対してオーバーライドされない場合に呼び出される。
        /// 結果の型によっては、nullを返すこともある。
        /// </summary>
        /// <param name="node">訪問する構文ノード</param>
        /// <returns>訪問の結果</returns>
        protected virtual TResult DefaultVisit(SyntaxNode node)
            => default!;

        /// <summary>
        /// スカラーノードを訪問するためのメソッド。
        /// </summary>
        /// <param name="node">訪問するスカラーノード</param>
        /// <returns>訪問の結果</returns>
        protected internal virtual TResult VisitScalar(ScalarNode node)
            => DefaultVisit(node);

        /// <summary>
        /// ブロックノードを訪問するためのメソッド。
        /// </summary>
        /// <param name="node">訪問するブロックノード</param>
        /// <returns>訪問の結果</returns>
        protected internal virtual TResult VisitBlock(BlockNode node)
            => DefaultVisit(node);

        /// <summary>
        /// プロパティノードを訪問するためのメソッド。
        /// </summary>
        /// <param name="node">訪問するプロパティノード</param>
        /// <returns>訪問の結果</returns>
        protected internal virtual TResult VisitProperty(PropertyNode node)
            => DefaultVisit(node);

        /// <summary>
        /// スカラープロパティノードを訪問するためのメソッド。デフォルトでは、プロパティノードの訪問処理を呼び出す。
        /// </summary>
        /// <param name="node">訪問するスカラープロパティノード</param>
        /// <returns>訪問の結果</returns>
        protected internal virtual TResult VisitScalarProperty(ScalarPropertyNode node)
            => VisitProperty(node);

        /// <summary>
        /// ブロックプロパティノードを訪問するためのメソッド。デフォルトでは、プロパティノードの訪問処理を呼び出す。
        /// </summary>
        /// <param name="node">訪問するブロックプロパティノード</param>
        /// <returns>訪問の結果</returns>
        protected internal virtual TResult VisitBlockProperty(BlockPropertyNode node)
            => VisitProperty(node);
        
        /// <summary>
        /// 型付きブロックプロパティノードを訪問するためのメソッド。デフォルトでは、プロパティノードの訪問処理を呼び出す。
        /// </summary>
        /// <param name="node">訪問する型付きブロックプロパティノード</param>
        /// <returns>訪問の結果</returns>
        protected internal virtual TResult VisitTypedBlockProperty(TypedBlockPropertyNode node)
            => VisitProperty(node);
        
        /// <summary>
        /// ルートノードを訪問するためのメソッド。
        /// </summary>
        /// <param name="node">訪問するルートノード</param>
        /// <returns>訪問の結果</returns>
        protected internal virtual TResult VisitRoot(RootNode node)
            => DefaultVisit(node);
    }
}