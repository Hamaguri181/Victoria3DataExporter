using PdxScriptAnalysis;
using PdxScriptAnalysis.Diagnostics;
using PdxScriptAnalysis.Syntax;
using PdxScriptAnalysis.Text;
using System.Diagnostics.CodeAnalysis;
using Victoria3.GameData;

namespace Victoria3.Loading.Loaders
{
    /// <summary>
    /// 解放可能国家のデータをロードするクラス。
    /// </summary>
    /// <param name="trees">読み込むスクリプトツリーのコレクション。</param>
    public class ReleasableCountryLoader(IEnumerable<ScriptTree> trees) : ILoader<ReleasableCountry>
    {
        private readonly IEnumerable<ScriptTree> _trees = trees;
        private readonly List<Diagnostic> _diagnostics = [];

        /// <inheritdoc/>
        public LoadOutput<ReleasableCountry> Load()
        {
            _diagnostics.Clear();
            var releasables = new List<ReleasableCountry>();

            foreach (var tree in _trees)
            {
                var releasablesFromTree = LoadFromTree(tree);
                releasables.AddRange(releasablesFromTree);
            }

            return new LoadOutput<ReleasableCountry>(releasables, _diagnostics);
        }

        private List<ReleasableCountry> LoadFromTree(ScriptTree tree)
        {
            var releasables = new List<ReleasableCountry>();

            foreach (var topLevelNode in tree.Root.Children)
            {
                if (topLevelNode is not BlockPropertyNode blockNode)
                {
                    AddError($"Unexpected top-level node of type {topLevelNode.GetType().Name}. Expected a BlockPropertyNode representing a releasable country definition.", topLevelNode.Span, topLevelNode.LinePosition);
                    continue;
                }

                if (TryLoadReleasableCountry(blockNode, out var releasableCountry))
                {
                    releasables.Add(releasableCountry);
                }
            }

            return releasables;
        }

        private bool TryLoadReleasableCountry(BlockPropertyNode node, [NotNullWhen(true)] out ReleasableCountry releasableCountry)
        {
            var releasableCountryBuilder = new ReleasableCountryBuilder();

            var tag = node.Key.Text;
            releasableCountryBuilder.Tag = tag;

            foreach (var child in node.Value.Children)
            {
                if (child is not PropertyNode propertyNode)
                {
                    AddError($"Unexpected child node of type {child.GetType().Name}. Expected a PropertyNode.", child.Span, child.LinePosition);
                    continue;
                }

                switch (propertyNode.Key.Text)
                {
                    case "states":
                    case "STATES":
                        if (TryParseToStringList(propertyNode, out var states)) releasableCountryBuilder.States = states;
                        break;
                    case "provinces":
                        if (TryParseToStringList(propertyNode, out var provinces)) releasableCountryBuilder.Provinces = provinces;
                        break;
                    case "use_culture_states":
                        if (TryParseToBool(propertyNode, out var useCultureStates)) releasableCountryBuilder.UseCultureStates = useCultureStates;
                        break;
                    case "required_num_states":
                        if (TryParseToInt(propertyNode, out var requiredNumStates)) releasableCountryBuilder.RequiredNumStates = requiredNumStates;
                        break;
                    case "ai_will_do":
                        releasableCountryBuilder.AIWillDo = propertyNode;
                        break;
                    case "possible":
                        releasableCountryBuilder.Possible = propertyNode;
                        break;
                    default:
                        AddWarning($"Unexpected property \"{propertyNode.Key.Text}\" in country definition. This property will be ignored.", propertyNode.Key.Span, propertyNode.LinePosition);
                        break;
                }
            }

            var missings = releasableCountryBuilder.GetMissingRequiredProperties();
            if (missings.Count > 0)
            {
                AddError($"Missing required properties for formable country with tag \"{tag}\": {string.Join(", ", missings)}.", node.Span, node.LinePosition);
                releasableCountry = default!;
                return false;
            }

            releasableCountry = releasableCountryBuilder.Build();
            return true;
        }


        private bool TryParseToStringList(PropertyNode node, [NotNullWhen(true)] out List<string> values)
        {
            if (PropertyNodeParsers.TryParseToStringList(node, out values, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic);
                values = null!;
                return false;
            }
        }

        private bool TryParseToBool(PropertyNode node, out bool value)
        {
            if (PropertyNodeParsers.TryParseToBool(node, out value, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic);
                value = false;
                return false;
            }
        }

        private bool TryParseToInt(PropertyNode node, out int value)
        {
            if (PropertyNodeParsers.TryParseToInt(node, out value, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic);
                value = 0;
                return false;
            }
        }

        // エラー診断を追加するためのヘルパーメソッド
        private void AddError(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, message, span, linePosition));

        private void AddWarning(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, message, span, linePosition));

        // ビルダー。必須プロパティを null 許容型で保持し、ビルド時に不足しているプロパティをチェックする。
        private class ReleasableCountryBuilder
        {
            internal string? Tag { get; set; }
            internal List<string> States { get; set; } = [];
            internal List<string> Provinces { get; set; } = [];
            internal bool? UseCultureStates { get; set; }
            internal int? RequiredNumStates { get; set; }
            internal object? AIWillDo { get; set; }
            internal object? Possible { get; set; }

            internal ReleasableCountry Build()
                => new(
                    Tag: Tag!,
                    States: States,
                    Provinces: Provinces,
                    UseCultureStates: UseCultureStates ?? false,
                    RequiredNumStates: RequiredNumStates,
                    AIWillDo: AIWillDo,
                    Possible: Possible
                    );

            internal List<string> GetMissingRequiredProperties()
            {
                var missingProperties = new List<string>();
                if (Tag is null) missingProperties.Add("Tag");
                return missingProperties;
            }
        }
    }
}
