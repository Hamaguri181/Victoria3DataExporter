using PdxScriptAnalysis;
using PdxScriptAnalysis.Diagnostics;
using Victoria3.GameData;

namespace Victoria3.Loading.Loaders
{
    /// <summary>
    /// 名前付きカラーを <see cref="ScriptTree"/> から読み込むローダー。
    /// </summary>
    /// <param name="trees">スクリプトツリーのコレクション</param>
    public sealed class NamedColorLoader(IEnumerable<ScriptTree> trees) : ILoader<NamedColor>
    {
        private readonly IEnumerable<ScriptTree> _trees = trees;
        public LoadOutput<NamedColor> Load()
        {
            var colors = new List<NamedColor>();
            var diagnostics = new List<Diagnostic>();

            foreach (var tree in _trees)
            {
                var output = new NamedColorTreeLoader(tree).Load();
                colors.AddRange(output.Values);
                diagnostics.AddRange(output.Diagnostics);
            }
            return new(colors, diagnostics);
        }
    }
}
