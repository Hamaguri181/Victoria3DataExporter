using PdxScriptAnalysis.Diagnostics;

namespace Victoria3.Loading
{
    /// <summary>
    /// ロードの出力を表すレコード。値と診断情報を含む。
    /// </summary>
    /// <typeparam name="T">ロードされるゲームデータの型。</typeparam>
    /// <param name="Values">ロードされたゲームデータのリスト。</param>
    /// <param name="Diagnostics">ロード中に発生した診断情報のリスト。</param>
    public sealed record LoadOutput<T>(
        IReadOnlyList<T> Values,
        IReadOnlyList<Diagnostic> Diagnostics);
}
