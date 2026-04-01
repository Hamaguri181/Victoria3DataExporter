using PdxScriptAnalysis;
using PdxScriptAnalysis.Diagnostics;
using PdxScriptAnalysis.Syntax;
using PdxScriptAnalysis.Text;
using System.Diagnostics.CodeAnalysis;
using Victoria3.GameData;

namespace Victoria3.Loading.Loaders
{
    /// <summary>
    /// 歴史的州地域のデータをロードするクラス。
    /// </summary>
    /// <param name="trees">読み込むスクリプトツリーのコレクション。</param>
    public class HistoricalStateRegionLoader(IEnumerable<ScriptTree> trees) : ILoader<HistoricalStateRegion>
    {
        private readonly IEnumerable<ScriptTree> _trees = trees;
        private readonly List<Diagnostic> _diagnostics = [];

        /// <inheritdoc/>
        public LoadOutput<HistoricalStateRegion> Load()
        {
            _diagnostics.Clear();
            var historicalStateRegions = new List<HistoricalStateRegion>();

            foreach (var tree in _trees)
            {
                var historicalStateRegionsFromTree = LoadFromTree(tree);
                historicalStateRegions.AddRange(historicalStateRegionsFromTree);
            }

            return new LoadOutput<HistoricalStateRegion>(historicalStateRegions, _diagnostics);
        }

        private List<HistoricalStateRegion> LoadFromTree(ScriptTree tree)
        {
            var historicalStateRegions = new List<HistoricalStateRegion>();

            if (tree.Root.Children.Count != 1)
            {
                AddError($"Expected exactly one top-level node in the script tree, but found {tree.Root.Children.Count}.", tree.Root.Span, tree.Root.LinePosition);
                return historicalStateRegions;
            }
            var topLevelNode = tree.Root.Children[0];
            if (topLevelNode is not BlockPropertyNode blockPropertyNode)
            {
                AddError($"Unexpected top-level node of type {topLevelNode.GetType().Name}. Expected a BlockPropertyNode representing the root of the historical state region definition.", topLevelNode.Span, topLevelNode.LinePosition);
                return historicalStateRegions;
            }
            if (blockPropertyNode.Key.Text != "STATES")
            {
                AddError($"Unexpected top-level block with key \"{blockPropertyNode.Key.Text}\". Expected a block with the key \"STATES\" representing the root of the historical state region definition.", blockPropertyNode.Key.Span, blockPropertyNode.Key.LinePosition);
                return historicalStateRegions;
            }

            foreach (var node in blockPropertyNode.Value.Children)
            {
                if (node is not BlockPropertyNode blockNode)
                {
                    AddError($"Unexpected child node of type {node.GetType().Name} under the top-level STATES block. Expected a BlockPropertyNode representing a historical state region definition.", node.Span, node.LinePosition);
                    continue;
                }

                if (TryLoadHistoricalStateRegion(blockNode, out var historicalStateRegion))
                {
                    historicalStateRegions.Add(historicalStateRegion);
                }
            }

            return historicalStateRegions;
        }

        private bool TryLoadHistoricalStateRegion(BlockPropertyNode node, [NotNullWhen(true)] out HistoricalStateRegion historicalStateRegion)
        {
            var historicalStateRegionBuilder = new HistoricalStateRegionBuilder();

            var tag = node.Key.Text;
            historicalStateRegionBuilder.Tag = tag;

            foreach (var child in node.Value.Children)
            {
                if (child is not PropertyNode propertyNode)
                {
                    AddError($"Unexpected child node of type {child.GetType().Name}. Expected a PropertyNode.", child.Span, child.LinePosition);
                    continue;
                }

                switch (propertyNode.Key.Text)
                {
                    case "create_state":
                        if (TryParseToCreateState(propertyNode, out var createState)) historicalStateRegionBuilder.CreateStates.Add(createState);
                        break;
                    case "add_homeland":
                        if (TryParseToString(propertyNode, out var homeland)) historicalStateRegionBuilder.Homelands.Add(homeland);
                        break;
                    case "add_claim":
                        if (TryParseToString(propertyNode, out var claim)) historicalStateRegionBuilder.Claims.Add(claim);
                        break;
                    default:
                        AddWarning($"Unexpected property \"{propertyNode.Key.Text}\" in country definition. This property will be ignored.", propertyNode.Key.Span, propertyNode.LinePosition);
                        break;
                }
            }

            var missings = historicalStateRegionBuilder.GetMissingRequiredProperties();
            if (missings.Count > 0)
            {
                AddError($"Missing required properties for formable country with tag \"{tag}\": {string.Join(", ", missings)}.", node.Span, node.LinePosition);
                historicalStateRegion = default!;
                return false;
            }

            historicalStateRegion = historicalStateRegionBuilder.Build();
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

        private bool TryParseToCreateState(PropertyNode node, [NotNullWhen(true)] out CreateState value)
        {
            if (node is not BlockPropertyNode createStateBlockNode)
            {
                AddError($"Expected a block property node for \"{node.Key.Text}\", but found a different type of node.", node.Span, node.LinePosition);
                value = null!;
                return false;
            }
            var createStateNodes = createStateBlockNode.Value.Children;
            if (!(createStateNodes.Count == 2 || createStateNodes.Count == 3))
            {
                AddError($"Expected exactly 2 or 3 child nodes under the \"{node.Key.Text}\" block for state creation definition, but found {createStateBlockNode.Value.Children.Count}.", createStateBlockNode.Span, createStateBlockNode.LinePosition);
                value = null!;
                return false;
            }
            if (!(createStateNodes.Any(n => n is ScalarPropertyNode scalar && scalar.Key.Text == "country") && createStateNodes.Any(n => n is BlockPropertyNode block && block.Key.Text == "owned_provinces")))
            {
                AddError($"Expected exactly one scalar property node with key \"country\" and one block property node with key \"owned_provinces\" under the \"create_state\" block for state creation definition, but the expected nodes were not found.", createStateBlockNode.Span, createStateBlockNode.LinePosition);
                value = null!;
                return false;
            }
            var country = createStateNodes
                .OfType<ScalarPropertyNode>()
                .FirstOrDefault(n => n.Key.Text == "country")?
                .Value.Token.Text;
            if (country is null)
            {
                AddError($"Expected a scalar property node with key \"country\" under the \"create_state\" block for state creation definition, but it was not found.", createStateBlockNode.Span, createStateBlockNode.LinePosition);
                value = null!;
                return false;
            }
            var provincesNode = createStateNodes
                .OfType<BlockPropertyNode>()
                .FirstOrDefault(n => n.Key.Text == "owned_provinces");
            if (provincesNode is null)
            {
                AddError($"Expected a block property node with key \"owned_provinces\" under the \"create_state\" block for state creation definition, but it was not found.", createStateBlockNode.Span, createStateBlockNode.LinePosition);
                value = null!;
                return false;
            }
            if (provincesNode.Value.Children.Any(c => c is not ScalarNode))
            {
                AddError($"Expected all child nodes under the \"owned_provinces\" block to be scalar nodes representing province IDs, but found child nodes of different types.", provincesNode.Span, provincesNode.LinePosition);
                value = null!;
                return false;
            }
            if (createStateNodes.Count == 3 && !createStateNodes.Any(n => n is ScalarPropertyNode scalar && scalar.Key.Text == "state_type"))
            {
                AddError($"Expected a scalar property node with key \"state_type\" as the optional third child node under the \"create_state\" block for state creation definition, but it was not found.", createStateBlockNode.Span, createStateBlockNode.LinePosition);
                value = null!;
                return false;
            }
            var stateType = createStateNodes
                .OfType<ScalarPropertyNode>()
                .FirstOrDefault(n => n.Key.Text == "state_type")?
                .Value.Token.Text;
            var provinces = provincesNode
                .Value.Children
                .OfType<ScalarNode>()
                .Select(n => n.Token.Text)
                .ToList();

            value = new CreateState(country, stateType, provinces);
            return true;
        }

        // エラー診断を追加するためのヘルパーメソッド
        private void AddError(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, message, span, linePosition));

        private void AddWarning(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, message, span, linePosition));

        // 歴史的州地域のビルダークラス。必須プロパティを null 許容型で保持し、ビルド時に不足しているプロパティをチェックする。
        private class HistoricalStateRegionBuilder
        {
            internal string? Tag { get; set; }
            internal List<CreateState> CreateStates { get; set; } = [];
            internal List<string> Homelands { get; set; } = [];
            internal List<string> Claims { get; set; } = [];

            internal HistoricalStateRegion Build()
                => new(
                    Tag: Tag!,
                    CreateStates: CreateStates,
                    Homelands: Homelands,
                    Claims: Claims
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
