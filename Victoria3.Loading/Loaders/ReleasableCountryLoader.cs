using PdxScriptAnalysis;
using PdxScriptAnalysis.Diagnostics;
using PdxScriptAnalysis.Syntax;
using PdxScriptAnalysis.Text;
using System.Diagnostics.CodeAnalysis;
using Victoria3.GameData;

namespace Victoria3.Loading.Loaders
{
    public class ReleasableCountryLoader(IEnumerable<ScriptTree> trees)
    {
        private readonly IEnumerable<ScriptTree> _trees = trees;
        private readonly List<Diagnostic> _diagnostics = [];

        /// <summary>
        /// 国家データをスクリプトツリーから読み込むメソッド。各ツリーを処理し、国家データのリストと診断情報を含む <see cref="LoadOutput{Country}"/> を返す。
        /// </summary>
        /// <returns>読み込まれた国家データと診断情報を含む <see cref="LoadOutput{Country}"/> オブジェクト</returns>
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
                        if (TryParseToStringList(propertyNode, "states", out var states))
                        {
                            releasableCountryBuilder.States = states;
                        }
                        break;
                    case "provinces":
                        if (TryParseToStringList(propertyNode, "provinces", out var provinces))
                        {
                            releasableCountryBuilder.Provinces = provinces;
                        }
                        break;
                    case "use_culture_states":
                        if (TryParseToBool(propertyNode, "use_culture_states", out var useCultureStates))
                        {
                            releasableCountryBuilder.UseCultureStates = useCultureStates;
                        }
                        break;
                    case "required_num_states":
                        if (TryParseToInt(propertyNode, "required_num_states", out var requiredNumStates))
                        {
                            releasableCountryBuilder.RequiredNumStates = requiredNumStates;
                        }
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


        private bool TryParseToInt(PropertyNode node, string propertyName, out int value)
        {
            if (node is not ScalarPropertyNode scalarPropertyNode)
            {
                AddError($"Expected a scalar property node for property \"{propertyName}\", but found a different type of node.", node.Span, node.LinePosition);
                value = default;
                return false;
            }
            if (!int.TryParse(scalarPropertyNode.Value.Token.Text, out value))
            {
                AddError($"Expected the value of property \"{propertyName}\" to be a valid integer number, but found \"{scalarPropertyNode.Value.Token.Text}\".", scalarPropertyNode.Value.Span, scalarPropertyNode.Value.LinePosition);
                return false;
            }
            return true;
        }

        // エラー診断を追加するためのヘルパーメソッド
        private void AddError(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, message, span, linePosition));

        private void AddWarning(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, message, span, linePosition));

        // 解放可能国家のビルダークラス。必須プロパティを null 許容型で保持し、ビルド時に不足しているプロパティをチェックする。
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
