using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis
{
    /// <summary>
    /// Lexerによって解析されたトークン。
    /// </summary>
    /// <param name="Kind">トークンの種類</param>
    /// <param name="Text">トークンのテキスト</param>
    /// <param name="Span">トークンの位置情報</param>
    public readonly record struct SyntaxToken(SyntaxKind Kind, string Text, TextSpan Span)
    {
        /// <summary>
        /// ファイルの終端を表すトークンかどうか。
        /// </summary>
        public bool IsEndOfFile => Kind == SyntaxKind.EndOfFile;

        /// <summary>
        /// 不明なトークンかどうか。
        /// </summary>
        public bool IsUnknown => Kind == SyntaxKind.Unknown;

        /// <summary>
        /// 演算子トークンかどうか。
        /// </summary>
        public bool IsOperator => Kind is SyntaxKind.Equals or SyntaxKind.LessThan or SyntaxKind.GreaterThan or SyntaxKind.LessThanEquals or SyntaxKind.GreaterThanEquals or SyntaxKind.NotEquals or SyntaxKind.QuestionEquals;


        /// <summary>
        /// 整数に変換できるトークンかどうか。整数に変換できる場合は、valueに変換された整数が格納される。
        /// </summary>
        /// <param name="value">変換された整数が格納される変数</param>
        /// <returns>整数に変換できる場合はtrue、それ以外の場合はfalse</returns>
        public bool TryGetInt(out int value)
            => int.TryParse(Text, out value);

        /// <summary>
        /// 十進数に変換できるトークンかどうか。十進数に変換できる場合は、valueに変換された十進数が格納される。
        /// </summary>
        /// <param name="value">変換された十進数が格納される変数</param>
        /// <returns>十進数に変換できる場合はtrue、それ以外の場合はfalse</returns>
        public bool TryGetDecimal(out decimal value)
            => decimal.TryParse(Text, out value);

        /// <summary>
        /// 真偽値に変換できるトークンかどうか。真偽値に変換できる場合は、valueに変換された真偽値が格納される。
        /// </summary>
        /// <param name="value">変換された真偽値が格納される変数</param>
        /// <returns>真偽値に変換できる場合はtrue、それ以外の場合はfalse</returns>
        public bool TryGetBool(out bool value)
        {
            if (Text.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }
            else if (Text.Equals("no", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// 二重引用符で囲まれた文字列リテラルから、引用符を除いた文字列を取得できるかどうか。文字列リテラルであれば、valueに引用符を除いた文字列が格納される。
        /// </summary>
        /// <param name="value">取得された文字列が格納される変数</param>
        /// <returns>文字列リテラルから文字列を取得できる場合はtrue、それ以外の場合はfalse</returns>
        public bool TryGetString(out string value)
        {
            if (Kind == SyntaxKind.StringLiteral && Text.Length >= 2 && Text[0] == '"' && Text[^1] == '"')
            {
                value = Text[1..^1];
                return true;
            }
            else
            {
                value = default!;
                return false;
            }
        }

        public override string ToString() => $"{Kind} \"{Text}\" {Span}";
    }
}
