using PdxScriptAnalysis;
using PdxScriptAnalysis.Diagnostics;
using PdxScriptAnalysis.Syntax;
using PdxScriptAnalysis.Text;
using Victoria3.GameData;

namespace Victoria3.Loading.Loaders
{
    /// <summary>
    /// 国家データを <see cref="ScriptTree"/> から読み込むローダー。
    /// </summary>
    /// <param name="trees">読み込むスクリプトツリーのコレクション。</param>
    public sealed class CountryLoader(IEnumerable<ScriptTree> trees)
    {
        private readonly IEnumerable<ScriptTree> _trees = trees;
        private readonly List<Diagnostic> _diagnostics = [];

        /// <summary>
        /// 国家データをスクリプトツリーから読み込むメソッド。各ツリーを処理し、国家データのリストと診断情報を含む <see cref="LoadOutput{Country}"/> を返す。
        /// </summary>
        /// <returns>読み込まれた国家データと診断情報を含む <see cref="LoadOutput{Country}"/> オブジェクト</returns>
        public LoadOutput<Country> Load()
        {
            _diagnostics.Clear();
            var countries = new List<Country>();

            foreach (var tree in _trees)
            {
                var countriesFromTree = LoadFromTree(tree);
                countries.AddRange(countriesFromTree);
            }

            return new LoadOutput<Country>(countries, _diagnostics);
        }

        private List<Country> LoadFromTree(ScriptTree tree)
        {
            var countries = new List<Country>();

            foreach (var topLevelNode in tree.Root.Children)
            {
                if (topLevelNode is not BlockPropertyNode blockNode)
                {
                    AddError($"Unexpected top-level node of type {topLevelNode.GetType().Name}. Expected a BlockPropertyNode representing a country definition.", topLevelNode.Span);
                    continue;
                }

                if (TryLoadCountry(blockNode, out var country))
                {
                    countries.Add(country);
                }
            }

            return countries;
        }

        private bool TryLoadCountry(BlockPropertyNode node, out Country country)
        {
            var countryBuilder = new CountryBuilder();

            var tag = node.Key.Text;
            countryBuilder.Tag = tag;

            foreach (var child in node.Value.Children)
            {
                if (child is not PropertyNode propertyNode)
                {
                    AddError($"Unexpected child node of type {child.GetType().Name}. Expected a PropertyNode.", child.Span);
                    continue;
                }

                switch (propertyNode.Key.Text)
                {
                    case "color":
                        if (TryParseToGameColor(propertyNode, "color", out var color))
                        {
                            countryBuilder.Color = color;
                        }
                        break;
                    case "type":
                        if (TryParseToString(propertyNode, "country_type", out var typeValue))
                        {
                            var originalTypeValue = typeValue;
                            typeValue = typeValue?.Replace("_", "", StringComparison.OrdinalIgnoreCase);
                            if (Enum.TryParse<CountryType>(typeValue, ignoreCase: true, out var type))
                            {
                                countryBuilder.Type = type;
                            }
                            else
                            {
                                AddError($"Invalid value \"{originalTypeValue}\" for property \"type\". Expected one of the following values: {string.Join(", ", Enum.GetNames<CountryType>())}.", propertyNode.Span);
                            }
                        }
                        break;
                    case "tier":
                        if (TryParseToString(propertyNode, "tier", out var tierValue))
                        {
                            var originalTierValue = tierValue;
                            tierValue = tierValue?.Replace("_", "", StringComparison.OrdinalIgnoreCase);
                            if (Enum.TryParse<CountryTier>(tierValue, ignoreCase: true, out var tier))
                            {
                                countryBuilder.Tier = tier;
                            }
                            else
                            {
                                AddError($"Invalid value \"{originalTierValue}\" for property \"tier\". Expected one of the following values: {string.Join(", ", Enum.GetNames<CountryTier>())}.", propertyNode.Span);
                            }
                        }
                        break;
                    case "social_hierarchy":
                        if (TryParseToString(propertyNode, "social_hierarchy", out var socialHierarchy))
                        {
                            countryBuilder.SocialHierarchy = socialHierarchy;
                        }
                        break;
                    case "religion":
                        if (TryParseToString(propertyNode, "religion", out var religion))
                        {
                            countryBuilder.Religion = religion;
                        }
                        break;
                    case "cultures":
                        if (TryParseToStringList(propertyNode, "cultures", out var cultures))
                        {
                            countryBuilder.Cultures = cultures;
                        }
                        break;
                    case "capital":
                        if (TryParseToString(propertyNode, "capital", out var capital))
                        {
                            countryBuilder.Capital = capital;
                        }
                        break;
                    case "is_named_from_capital":
                        if (TryParseToBool(propertyNode, "is_named_from_capital", out var isNamedFromCapital))
                        {
                            countryBuilder.IsNamedFromCapital = isNamedFromCapital;
                        }
                        break;
                    case "valid_as_home_country_for_separatists":
                        // 一旦ノードをそのまま
                        countryBuilder.ValidAsHomeCountryForSeparatists = propertyNode;
                        break;
                    case "primary_unit_color":
                        if (TryParseToGameColor(propertyNode, "primary_unit_color", out var primaryUnitColor))
                        {
                            countryBuilder.PrimaryUnitColor = primaryUnitColor;
                        }
                        break;
                    case "secondary_unit_color":
                        if (TryParseToGameColor(propertyNode, "secondary_unit_color", out var secondaryUnitColor))
                        {
                            countryBuilder.SecondaryUnitColor = secondaryUnitColor;
                        }
                        break;
                    case "tertiary_unit_color":
                        if (TryParseToGameColor(propertyNode, "tertiary_unit_color", out var tertiaryUnitColor))
                        {
                            countryBuilder.TertiaryUnitColor = tertiaryUnitColor;
                        }
                        break;
                    case "dynamic_country_definition":
                        // dynamic_country_definition = yes のプロパティを持つ場合その国家は読み取らない
                        if (TryParseToBool(propertyNode, "dynamic_country_definition", out var isDynamicCountryDefinition) && isDynamicCountryDefinition == true)
                        {
                            country = default!;
                            return false;
                        }
                        break;
                    default:
                        AddWarning($"Unexpected property \"{propertyNode.Key.Text}\" in country definition. This property will be ignored.", propertyNode.Key.Span);
                        break;
                }
            }

            var missings = countryBuilder.GetMissingRequiredProperties();
            if (missings.Count > 0)
            {
                AddError($"Missing required properties for country with tag \"{tag}\": {string.Join(", ", missings)}.", node.Span);
                country = default!;
                return false;
            }

            country = countryBuilder.Build();
            return true;
        }


        // スカラープロパティノードの右辺を文字列として解析するためのヘルパーメソッド
        private bool TryParseToString(PropertyNode node, string propertyName, out string? value)
        {
            if (node is not ScalarPropertyNode scalarPropertyNode)
            {
                AddError($"Expected a scalar property node for property \"{propertyName}\", but found a different type of node.", node.Span);
                value = null;
                return false;
            }

            value = scalarPropertyNode.Value.Token.Text;
            return true;
        }

        // ブロックプロパティノードの右辺を文字列のリストとして解析するためのヘルパーメソッド
        private bool TryParseToStringList(PropertyNode node, string propertyName, out List<string> values)
        {
            if (node is not BlockPropertyNode blockPropertyNode)
            {
                AddError($"Expected a block property node for property \"{propertyName}\", but found a different type of node.", node.Span);
                values = [];
                return false;
            }

            if (blockPropertyNode.Value.Children.Any(c => c is not ScalarNode))
            {
                AddError($"Expected all children of the block for property \"{propertyName}\" to be scalar nodes representing string values, but found child nodes of different types.", blockPropertyNode.Span);
                values = [];
                return false;
            }

            values = blockPropertyNode.Value.Children
                .OfType<ScalarNode>()
                .Select(n => n.Token.Text)
                .ToList();
            return true;
        }

        // スカラープロパティノードの右辺を真偽値として解析するためのヘルパーメソッド
        private bool TryParseToBool(PropertyNode node, string propertyName, out bool? value)
        {
            if (node is not ScalarPropertyNode scalarPropertyNode)
            {
                AddError($"Expected a scalar property node for property \"{propertyName}\", but found a different type of node.", node.Span);
                value = default;
                return false;
            }

            value = scalarPropertyNode.Value.Token.Text switch
            {
                "yes" => true,
                "no" => false,
                _ => null
            };
            if (value is null)
            {
                AddError($"Expected the value of property \"{propertyName}\" to be \"yes\" or \"no\", but found \"{scalarPropertyNode.Value.Token.Text}\".", scalarPropertyNode.Value.Span);
                return false;
            }
            return value.HasValue;
        }

        // ブロックプロパティノードまたは型付きブロックプロパティノードの右辺を GameColor として解析するためのヘルパーメソッド
        private bool TryParseToGameColor(PropertyNode node, string propertyName, out GameColor? color)
        {
            switch (node)
            {
                case BlockPropertyNode block:
                    if (TryParseFromBlockToGameColor(block.Value, propertyName, out var colorValues))
                    {
                        color = ColorConverter.FromRgb(colorValues[0], colorValues[1], colorValues[2]);
                        return true;
                    }
                    else
                    {
                        color = null;
                        return false;
                    }
                case TypedBlockPropertyNode typedBlock:
                    var typeQualifier = typedBlock.TypeQualifier.Text;
                    if (TryParseFromBlockToGameColor(typedBlock.Value, propertyName, out var typedColorValues))
                    {
                        if (typeQualifier.Equals("hsv", StringComparison.OrdinalIgnoreCase))
                        {
                            color = ColorConverter.FromHsv((float)typedColorValues[0], (float)typedColorValues[1], (float)typedColorValues[2]);
                        }
                        else if (typeQualifier.Equals("hsv360", StringComparison.OrdinalIgnoreCase))
                        {
                            color = ColorConverter.FromHsv360(typedColorValues[0], typedColorValues[1], typedColorValues[2]);
                        }
                        else if (typeQualifier.Equals("rgb", StringComparison.OrdinalIgnoreCase))
                        {
                            color = ColorConverter.FromRgb(typedColorValues[0], typedColorValues[1], typedColorValues[2]);
                        }
                        else
                        {
                            AddError($"Unsupported color type qualifier \"{typeQualifier}\" for property \"{propertyName}\". Expected \"rgb\", \"hsv\", or \"hsv360\".", typedBlock.TypeQualifier.Span);
                            color = null;
                            return false;
                        }
                        return true;
                    }
                    else
                    {
                        color = null;
                        return false;
                    }
                default:
                    AddError($"Expected a block or typed block property node for property \"{propertyName}\", but found a different type of node.", node.Span);
                    color = null;
                    return false;
            }

        }

        // ブロックノードの子ノードを色の値として解析するためのヘルパーメソッド
        private bool TryParseFromBlockToGameColor(BlockNode block, string propertyName, out decimal[] colorValues)
        {
            if (block.Children.Count != 3)
            {
                AddError($"Expected a block with exactly 3 children for property \"{propertyName}\" to represent RGB values, but found a block with {block.Children.Count} children.", block.Span);
                colorValues = [];
                return false;
            }

            if (block.Children.Any(c => c is not ScalarNode))
            {
                AddError($"Expected all children of the block for property \"{propertyName}\" to be scalar nodes representing RGB components, but found a child node of a different type.", block.Span);
                colorValues = [];
                return false;
            }

            var rgbValueNodes = block.Children.OfType<ScalarNode>().ToList();

            colorValues = new decimal[3];
            for (int i = 0; i < 3; i++)
            {
                if (!decimal.TryParse(rgbValueNodes[i].Token.Text, out colorValues[i]))
                {
                    AddError($"Expected the value of child node {i + 1} of the block for property \"{propertyName}\" to be a valid byte (0-255) representing an RGB component, but found \"{rgbValueNodes[i].Token.Text}\".", rgbValueNodes[i].Span);
                    colorValues = [];
                    return false;
                }
            }
            return true;
        }

        // エラー診断を追加するためのヘルパーメソッド
        private void AddError(string message, TextSpan span)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, message, span));

        private void AddWarning(string message, TextSpan span)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, message, span));


        // 国のビルダークラス。必須プロパティを null 許容型で保持し、ビルド時に不足しているプロパティをチェックする。
        private class CountryBuilder
        {
            internal string? Tag { get; set; }
            internal GameColor? Color { get; set; }
            internal CountryType? Type { get; set; }
            internal CountryTier? Tier { get; set; }
            internal string? SocialHierarchy { get; set; }
            internal string? Religion { get; set; }
            internal List<string> Cultures { get; set; } = [];
            internal string? Capital { get; set; }
            internal bool? IsNamedFromCapital { get; set; }
            internal object? ValidAsHomeCountryForSeparatists { get; set; }
            internal GameColor? PrimaryUnitColor { get; set; }
            internal GameColor? SecondaryUnitColor { get; set; }
            internal GameColor? TertiaryUnitColor { get; set; }


            internal Country Build()
                => new(
                    Tag: Tag!,
                    Color: Color!.Value,
                    Type: Type!.Value,
                    Tier: Tier!.Value,
                    SocialHierarchy: SocialHierarchy,
                    Religion: Religion,
                    Cultures: Cultures,
                    Capital: Capital!,
                    IsNamedFromCapital: IsNamedFromCapital ?? false,
                    ValidAsHomeCountryForSeparatists: ValidAsHomeCountryForSeparatists,
                    PrimaryUnitColor: PrimaryUnitColor,
                    SecondaryUnitColor: SecondaryUnitColor,
                    TertiaryUnitColor: TertiaryUnitColor);

            internal List<string> GetMissingRequiredProperties()
            {
                var missingProperties = new List<string>();
                if (Tag is null) missingProperties.Add("Tag");
                if (Color is null) missingProperties.Add("Color");
                if (Type is null) missingProperties.Add("Type");
                if (Tier is null) missingProperties.Add("Tier");
                if (Capital is null) missingProperties.Add("Capital");
                if (Cultures.Count == 0) missingProperties.Add("Cultures");
                return missingProperties;
            }
        }
    }
}
