using PdxScriptAnalysis;
using PdxScriptAnalysis.Diagnostics;
using PdxScriptAnalysis.Syntax;
using PdxScriptAnalysis.Text;
using System.Diagnostics.CodeAnalysis;
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
        private readonly List<Diagnostic> _diagnostics = [];

        ///  <inheritdoc/>
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

        // 1つのスクリプトツリーをロードする
        private List<Country> LoadFromTree(ScriptTree tree)
        {
            var countries = new List<Country>();

            foreach (var topLevelNode in tree.Root.Children)
            {
                if (topLevelNode is not BlockPropertyNode blockNode)
                {
                    AddError($"Unexpected top-level node of type {topLevelNode.GetType().Name}. Expected a BlockPropertyNode representing a country definition.", topLevelNode.Span, topLevelNode.LinePosition);
                    continue;
                }

                if (TryLoadCountry(blockNode, out var country))
                {
                    countries.Add(country);
                }
            }

            return countries;
        }

        private bool TryLoadCountry(BlockPropertyNode node, [NotNullWhen(true)] out Country country)
        {
            var countryBuilder = new CountryBuilder();

            var tag = node.Key.Text;
            countryBuilder.Tag = tag;

            foreach (var child in node.Value.Children)
            {
                if (child is not PropertyNode propertyNode)
                {
                    AddError($"Unexpected child node of type {child.GetType().Name}. Expected a PropertyNode.", child.Span, child.LinePosition);
                    continue;
                }

                switch (propertyNode.Key.Text)
                {
                    case "color":
                        if (TryParseToGameColor(propertyNode, out var color)) countryBuilder.Color = color;
                        break;
                    case "country_type":
                        if (TryParseToEnum<CountryType>(propertyNode, out var type)) countryBuilder.Type = type;
                        break;
                    case "tier":
                        if (TryParseToEnum<CountryTier>(propertyNode, out var tier)) countryBuilder.Tier = tier;
                        break;
                    case "social_hierarchy":
                        if (TryParseToString(propertyNode, out var socialHierarchy)) countryBuilder.SocialHierarchy = socialHierarchy;
                        break;
                    case "religion":
                        if (TryParseToString(propertyNode, out var religion)) countryBuilder.Religion = religion;
                        break;
                    case "cultures":
                        if (TryParseToStringList(propertyNode, out var cultures)) countryBuilder.Cultures = cultures;
                        break;
                    case "capital":
                        if (TryParseToString(propertyNode, out var capital)) countryBuilder.Capital = capital;
                        break;
                    case "is_named_from_capital":
                        if (TryParseToBool(propertyNode, out var isNamedFromCapital)) countryBuilder.IsNamedFromCapital = isNamedFromCapital;
                        break;
                    case "valid_as_home_country_for_separatists":
                        // 一旦ノードをそのまま
                        countryBuilder.ValidAsHomeCountryForSeparatists = propertyNode;
                        break;
                    case "primary_unit_color":
                        if (TryParseToGameColor(propertyNode, out var primaryUnitColor)) countryBuilder.PrimaryUnitColor = primaryUnitColor;
                        break;
                    case "secondary_unit_color":
                        if (TryParseToGameColor(propertyNode, out var secondaryUnitColor)) countryBuilder.SecondaryUnitColor = secondaryUnitColor;
                        break;
                    case "tertiary_unit_color":
                        if (TryParseToGameColor(propertyNode, out var tertiaryUnitColor)) countryBuilder.TertiaryUnitColor = tertiaryUnitColor;
                        break;
                    case "dynamic_country_definition":
                        // dynamic_country_definition = yes のプロパティを持つ場合その国家は読み取らない
                        if (TryParseToBool(propertyNode, out var isDynamicCountryDefinition) && isDynamicCountryDefinition == true)
                        {
                            country = default!;
                            return false;
                        }
                        break;
                    default:
                        AddWarning($"Unexpected property \"{propertyNode.Key.Text}\" in country definition. This property will be ignored.", propertyNode.Key.Span, propertyNode.LinePosition);
                        break;
                }
            }

            var missings = countryBuilder.GetMissingRequiredProperties();
            if (missings.Count > 0)
            {
                AddError($"Missing required properties for country with tag \"{tag}\": {string.Join(", ", missings)}.", node.Span, node.LinePosition);
                country = default!;
                return false;
            }

            country = countryBuilder.Build();
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

        private bool TryParseToEnum<TEnum>(PropertyNode node, out TEnum value)
            where TEnum : struct, Enum
        {
            if (PropertyNodeParsers.TryParseToEnum(node, out value, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic);
                value = default;
                return false;
            }
        }

        private bool TryParseToGameColor(PropertyNode node, out GameColor color)
        {
            if (PropertyNodeParsers.TryParseToGameColor(node, out color, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic);
                color = default;
                return false;
            }
        }

        // エラー診断を追加するためのヘルパーメソッド
        private void AddError(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, message, span, linePosition));
        private void AddWarning(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, message, span, linePosition));

        // ビルダー。必須プロパティを null 許容型で保持し、ビルド時に不足しているプロパティをチェックする。
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
                    Capital: Capital,
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
                if (Cultures.Count == 0) missingProperties.Add("Cultures");
                return missingProperties;
            }
        }
    }
}
