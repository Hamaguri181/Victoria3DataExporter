namespace PdxScriptAnalysis
{
    /// <summary>
    /// トークンの種類。
    /// </summary>
    public enum SyntaxKind
    {
        // 構造
        LeftBrace,
        RightBrace,

        // 演算子
        Equals,
        LessThan,
        GreaterThan,
        LessThanEquals,
        GreaterThanEquals,
        NotEquals,
        QuestionEquals,

        // リテラル
        StringLiteral,  // 二重引用符で囲まれた文字列 "..."

        // 識別子・数値
        Atom,

        // その他
        Unknown,
        EndOfFile,
    }
}