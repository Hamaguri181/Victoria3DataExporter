using PdxScriptAnalysis;
using PdxScriptAnalysis.Diagnostics;
using Victoria3.GameData;

namespace Victoria3.Loading.Loaders
{
    /// <summary>
    /// 国家データを <see cref="ScriptTree"/> から読み込むローダー。
    /// </summary>
    /// <param name="trees">読み込むスクリプトツリーのコレクション。</param>
    public sealed class CountryLoader(IEnumerable<ScriptTree> trees) : ILoader<Country>
    {
        private readonly IEnumerable<ScriptTree> _trees = trees;

        ///  <inheritdoc/>
        public LoadOutput<Country> Load()
        {
            var countries = new List<Country>();
            var diagnostics = new List<Diagnostic>();

            foreach (var tree in _trees)
            {
                var output = new CountryTreeLoader(tree).Load();
                countries.AddRange(output.Values);
                diagnostics.AddRange(output.Diagnostics);
            }

            return new(countries, diagnostics);
        }
    }
}
