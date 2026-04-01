using PdxScriptAnalysis;
using PdxScriptAnalysis.Diagnostics;
using PdxScriptAnalysis.Syntax;
using PdxScriptAnalysis.Text;
using System.Diagnostics.CodeAnalysis;
using Victoria3.GameData;

namespace Victoria3.Loading.Loaders
{
    /// <summary>
    /// 形成可能国家のロード処理を担当するクラス。
    /// </summary>
    /// <param name="trees">読み込むスクリプトツリーのコレクション。</param>
    public sealed class FormableCountryLoader(IEnumerable<ScriptTree> trees) : ILoader<FormableCountry>
    {
        private readonly IEnumerable<ScriptTree> _trees = trees;
        private readonly List<Diagnostic> _diagnostics = [];

        /// <inheritdoc/>
        public LoadOutput<FormableCountry> Load()
        {
            _diagnostics.Clear();
            var formables = new List<FormableCountry>();

            foreach (var tree in _trees)
            {
                var formablesFromTree = LoadFromTree(tree);
                formables.AddRange(formablesFromTree);
            }

            return new LoadOutput<FormableCountry>(formables, _diagnostics);
        }

        private List<FormableCountry> LoadFromTree(ScriptTree tree)
        {
            var formables = new List<FormableCountry>();

            foreach (var topLevelNode in tree.Root.Children)
            {
                if (topLevelNode is not BlockPropertyNode blockNode)
                {
                    AddError($"Unexpected top-level node of type {topLevelNode.GetType().Name}. Expected a BlockPropertyNode representing a formable country definition.", topLevelNode.Span, topLevelNode.LinePosition);
                    continue;
                }

                if (TryLoadFormableCountry(blockNode, out var formableCountry))
                {
                    formables.Add(formableCountry);
                }
            }

            return formables;
        }

        private bool TryLoadFormableCountry(BlockPropertyNode node, [NotNullWhen(true)] out FormableCountry formableCountry)
        {
            var formableCountryBuilder = new FormableCountryBuilder();

            var tag = node.Key.Text;
            formableCountryBuilder.Tag = tag;

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
                        if (TryParseToStringList(propertyNode, out var states)) formableCountryBuilder.States = states;
                        break;
                    case "use_culture_states":
                        if (TryParseToBool(propertyNode, out var useCultureStates)) formableCountryBuilder.UseCultureStates = useCultureStates;
                        break;
                    case "required_states_fraction":
                        if (TryParseToDecimal(propertyNode, out var requiredStatesFraction)) formableCountryBuilder.RequiredStatesFraction = requiredStatesFraction;
                        break;
                    case "ai_will_do":
                        formableCountryBuilder.AIWillDo = propertyNode;
                        break;
                    case "potential":
                        formableCountryBuilder.Potential = propertyNode;
                        break;
                    case "possible":
                        formableCountryBuilder.Possible = propertyNode;
                        break;
                    case "geographic_region":
                        if (TryParseToString(propertyNode, out var geographicRegion)) formableCountryBuilder.GeographicRegion = geographicRegion;
                        break;
                    case "is_major_formation":
                        if (TryParseToBool(propertyNode, out var isMajorFormation)) formableCountryBuilder.IsMajorFormation = isMajorFormation;
                        break;
                    case "unification_play":
                        if (TryParseToString(propertyNode, out var unificationPlay)) formableCountryBuilder.UnificationPlay = unificationPlay;
                        break;
                    case "leadership_play":
                        if (TryParseToString(propertyNode, out var leadershipPlay)) formableCountryBuilder.LeadershipPlay = leadershipPlay;
                        break;
                    case "max_num_formation_candidates":
                        if (TryParseToInt(propertyNode, out var maxNumFormationCandidates)) formableCountryBuilder.MaxNumFormationCandidates = maxNumFormationCandidates;
                        break;
                    case "can_be_formation_candidate":
                        formableCountryBuilder.CanBeFormationCandidate = propertyNode;
                        break;
                    case "can_be_unification_target":
                        formableCountryBuilder.CanBeUnificationTarget = propertyNode;
                        break;
                    default:
                        AddWarning($"Unexpected property \"{propertyNode.Key.Text}\" in country definition. This property will be ignored.", propertyNode.Key.Span, propertyNode.LinePosition);
                        break;
                }
            }

            var missings = formableCountryBuilder.GetMissingRequiredProperties();
            if (missings.Count > 0)
            {
                AddError($"Missing required properties for formable country with tag \"{tag}\": {string.Join(", ", missings)}.", node.Span, node.LinePosition);
                formableCountry = default!;
                return false;
            }

            formableCountry = formableCountryBuilder.Build();
            return true;
        }


        private bool TryParseToString(PropertyNode node, [NotNullWhen(true)] out string value)
        {
            if (PropertyNodeParsers.TryParseToString(node, out value, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic);
                value = null!;
                return false;
            }
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

        private bool TryParseToDecimal(PropertyNode node, out decimal value)
        {
            if (PropertyNodeParsers.TryParseToDecimal(node, out value, out var diagnostic))
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
        private class FormableCountryBuilder
        {
            internal string? Tag { get; set; }
            internal List<string> States { get; set; } = [];
            internal bool? UseCultureStates { get; set; }
            internal decimal? RequiredStatesFraction { get; set; }
            internal object? AIWillDo { get; set; }
            internal object? Potential { get; set; }
            internal object? Possible { get; set; }
            internal string? GeographicRegion { get; set; }
            internal bool? IsMajorFormation { get; set; }
            internal string? UnificationPlay { get; set; }
            internal string? LeadershipPlay { get; set; }
            internal int? MaxNumFormationCandidates { get; set; }
            internal object? CanBeFormationCandidate { get; set; }
            internal object? CanBeUnificationTarget { get; set; }

            internal FormableCountry Build()
                => new(
                    Tag: Tag!,
                    States: States,
                    UseCultureStates: UseCultureStates ?? false,
                    RequiredStatesFraction: RequiredStatesFraction ?? 1,
                    AIWillDo: AIWillDo,
                    Potential: Potential,
                    Possible: Possible,
                    GeographicRegion: GeographicRegion,
                    IsMajorFormation: IsMajorFormation ?? false,
                    UnificationPlay: UnificationPlay,
                    LeadershipPlay: LeadershipPlay,
                    MaxNumFormationCandidates: MaxNumFormationCandidates,
                    CanBeFormationCandidate: CanBeFormationCandidate,
                    CanBeUnificationTarget: CanBeUnificationTarget
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
