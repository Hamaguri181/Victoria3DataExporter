using PdxScriptAnalysis;
using PdxScriptAnalysis.Diagnostics;
using PdxScriptAnalysis.Syntax;
using PdxScriptAnalysis.Text;
using System.Diagnostics.CodeAnalysis;
using Victoria3.GameData;

namespace Victoria3.Loading.Loaders
{
    public sealed class FormableCountryLoader(IEnumerable<ScriptTree> trees)
    {
        private readonly IEnumerable<ScriptTree> _trees = trees;
        private readonly List<Diagnostic> _diagnostics = [];

        /// <summary>
        /// 国家データをスクリプトツリーから読み込むメソッド。各ツリーを処理し、国家データのリストと診断情報を含む <see cref="LoadOutput{Country}"/> を返す。
        /// </summary>
        /// <returns>読み込まれた国家データと診断情報を含む <see cref="LoadOutput{Country}"/> オブジェクト</returns>
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
                        if (TryParseToStringList(propertyNode, "states", out var states))
                        {
                            formableCountryBuilder.States = states;
                        }
                        break;
                    case "use_culture_states":
                        if (TryParseToBool(propertyNode, "use_culture_states", out var useCultureStates))
                        {
                            formableCountryBuilder.UseCultureStates = useCultureStates;
                        }
                        break;
                    case "required_states_fraction":
                        if (TryParseToDecimal(propertyNode, "required_states_fraction", out var requiredStatesFraction))
                        {
                            formableCountryBuilder.RequiredStatesFraction = requiredStatesFraction;
                        }
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
                        if (TryParseToString(propertyNode, "geographic_region", out var geographicRegion))
                        {
                            formableCountryBuilder.GeographicRegion = geographicRegion;
                        }
                        break;
                    case "is_major_formation":
                        if (TryParseToBool(propertyNode, "is_major_formation", out var isMajorFormation))
                        {
                            formableCountryBuilder.IsMajorFormation = isMajorFormation;
                        }
                        break;
                    case "unification_play":
                        if (TryParseToString(propertyNode, "unification_play", out var unificationPlay))
                        {
                            formableCountryBuilder.UnificationPlay = unificationPlay;
                        }
                        break;
                    case "leadership_play":
                        if (TryParseToString(propertyNode, "leadership_play", out var leadershipPlay))
                        {
                            formableCountryBuilder.LeadershipPlay = leadershipPlay;
                        }
                        break;
                    case "max_num_formation_candidates":
                        if (TryParseToDecimal(propertyNode, "max_num_formation_candidates", out var maxNumFormationCandidates))
                        {
                            formableCountryBuilder.MaxNumFormationCandidates = maxNumFormationCandidates;
                        }
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


        // スカラープロパティノードの右辺を文字列として解析するためのヘルパーメソッド
        private bool TryParseToString(PropertyNode node, string propertyName, [NotNullWhen(true)] out string value)
        {
            if (node is not ScalarPropertyNode scalarPropertyNode)
            {
                AddError($"Expected a scalar property node for property \"{propertyName}\", but found a different type of node.", node.Span, node.LinePosition);
                value = null!;
                return false;
            }

            value = scalarPropertyNode.Value.Token.Text;
            return true;
        }

        // ブロックプロパティノードの右辺を文字列のリストとして解析するためのヘルパーメソッド
        private bool TryParseToStringList(PropertyNode node, string propertyName, [NotNullWhen(true)] out List<string> values)
        {
            if (node is not BlockPropertyNode blockPropertyNode)
            {
                AddError($"Expected a block property node for property \"{propertyName}\", but found a different type of node.", node.Span, node.LinePosition);
                values = null!;
                return false;
            }

            if (blockPropertyNode.Value.Children.Any(c => c is not ScalarNode))
            {
                AddError($"Expected all children of the block for property \"{propertyName}\" to be scalar nodes representing string values, but found child nodes of different types.", blockPropertyNode.Span, blockPropertyNode.LinePosition);
                values = null!;
                return false;
            }

            values = blockPropertyNode.Value.Children
                .OfType<ScalarNode>()
                .Select(n => n.Token.Text)
                .ToList();
            return true;
        }

        // スカラープロパティノードの右辺を真偽値として解析するためのヘルパーメソッド
        private bool TryParseToBool(PropertyNode node, string propertyName, out bool value)
        {
            if (node is not ScalarPropertyNode scalarPropertyNode)
            {
                AddError($"Expected a scalar property node for property \"{propertyName}\", but found a different type of node.", node.Span, node.LinePosition);
                value = default;
                return false;
            }

            switch (scalarPropertyNode.Value.Token.Text)
            {
                case "yes":
                    value = true;
                    return true;
                case "no":
                    value = false;
                    return true;
                default:
                    AddError($"Expected the value of property \"{propertyName}\" to be \"yes\" or \"no\", but found \"{scalarPropertyNode.Value.Token.Text}\".", scalarPropertyNode.Value.Span, scalarPropertyNode.Value.LinePosition);
                    value = default;
                    return false;
            }
        }

        // スカラープロパティノードの右辺を十進数として解析するためのヘルパーメソッド
        private bool TryParseToDecimal(PropertyNode node, string propertyName, out decimal value)
        {
            if (node is not ScalarPropertyNode scalarPropertyNode)
            {
                AddError($"Expected a scalar property node for property \"{propertyName}\", but found a different type of node.", node.Span, node.LinePosition);
                value = default;
                return false;
            }
            if (!decimal.TryParse(scalarPropertyNode.Value.Token.Text, out value))
            {
                AddError($"Expected the value of property \"{propertyName}\" to be a valid decimal number, but found \"{scalarPropertyNode.Value.Token.Text}\".", scalarPropertyNode.Value.Span, scalarPropertyNode.Value.LinePosition);
                return false;
            }
            return true;
        }

        // エラー診断を追加するためのヘルパーメソッド
        private void AddError(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, message, span, linePosition));

        private void AddWarning(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, message, span, linePosition));

        // 形成可能国家のビルダークラス。必須プロパティを null 許容型で保持し、ビルド時に不足しているプロパティをチェックする。
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
            internal decimal? MaxNumFormationCandidates { get; set; }
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
                    MaxNumFormationCandidates: MaxNumFormationCandidates ?? 0,
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
