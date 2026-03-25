using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Syntax
{
    /// <summary>
    /// Key Operator Valueの形式を持つプロパティノード。
    /// Keyはプロパティの名前を表すトークン、Operatorはプロパティの演算子を表すトークンである。
    /// </summary>
    /// <param name="Key">プロパティの名前を表すトークン</param>
    /// <param name="Operator">プロパティの演算子を表すトークン</param>
    /// <param name="Span">ソーステキスト上の範囲を表すスパン</param>
    public abstract record PropertyNode(
        SyntaxToken Key,
        SyntaxToken Operator,
        TextSpan Span)
        : SyntaxNode(Span)
    {
        public override LinePosition LinePosition
            => Key.LinePosition;

        public override string ToString()
            => $"{GetType().Name}: {Key.Text} {Operator.Text} ... at {Span}";
    }
}
