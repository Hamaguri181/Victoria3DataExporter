using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Diagnostics
{
    /// <summary>
    /// 構文解析や意味解析の過程で発生する診断情報を表すクラス。
    /// </summary>
    /// <param name="Severity">診断の重大度を表す値。</param>
    /// <param name="Message">診断メッセージ。</param>
    /// <param name="Span">診断が発生したソースコードの範囲。</param>
    /// <param name="LinePosition">診断が発生した行位置情報。</param>
    public sealed record Diagnostic(
        DiagnosticSeverity Severity,
        string Message,
        TextSpan Span,
        LinePosition LinePosition)
    {
        /// <summary>
        /// 診断の重大度が情報であるかどうか。
        /// </summary>
        public bool IsInfo => Severity == DiagnosticSeverity.Info;

        /// <summary>
        /// 診断の重大度がエラーであるかどうか。
        /// </summary>
        public bool IsError => Severity == DiagnosticSeverity.Error;

        /// <summary>
        /// 診断の重大度が警告であるかどうか。
        /// </summary>
        public bool IsWarning => Severity == DiagnosticSeverity.Warning;

        public override string ToString()
                => $"{Severity}: {Message} at {LinePosition}";
    }
}
