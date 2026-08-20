using PdxScriptAnalysis;
using PdxScriptAnalysis.Diagnostics;
using Victoria3.GameData;

namespace Victoria3.Loading.Loaders
{
    /// <summary>
    /// 国家データを <see cref="ScriptTree"/> から読み込むローダー。
    /// </summary>
    /// <param name="trees">読み込むスクリプトツリーのコレクション。</param>
    /// <param name="namedColors">使用可能な名前付き色のコレクション。</param>
    public sealed class CountryLoader(IEnumerable<ScriptTree> trees, IEnumerable<NamedColor> namedColors) : ILoader<Country>
    {
        private readonly IEnumerable<ScriptTree> _trees = trees;
        private readonly IReadOnlyList<NamedColor> _namedColors = namedColors.ToList();

        ///  <inheritdoc/>
        public LoadOutput<Country> Load()
        {
            var countries = new List<Country>();
            var diagnostics = new List<Diagnostic>();

            foreach (var tree in _trees)
            {
                var output = new CountryTreeLoader(tree, _namedColors).Load();
                countries.AddRange(output.Values);
                diagnostics.AddRange(output.Diagnostics);
            }

            return new(countries, diagnostics);
        }
    }
}
