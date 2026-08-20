using PdxScriptAnalysis;
using PdxScriptAnalysis.Diagnostics;
using PdxScriptAnalysis.Syntax;
using PdxScriptAnalysis.Text;
using System.Diagnostics.CodeAnalysis;
using Victoria3.GameData;

namespace Victoria3.Loading.Loaders
{
    /// <summary>
    /// 1つの<see cref="ScriptTree"/>から国家データを読み込むための内部クラス。
    /// </summary>
    /// <param name="tree">読み込むスクリプトツリー。</param>
    /// <param name="namedColors">使用可能な名前付き色のコレクション。</param>
    internal sealed class CountryTreeLoader(ScriptTree tree, IReadOnlyList<NamedColor> namedColors)
    {
        private readonly ScriptTree _tree = tree;
        private readonly IReadOnlyList<NamedColor> _namedColors = namedColors;
        private readonly List<Diagnostic> _diagnostics = [];

        private string? FilePath => _tree.Source.FilePath;


        /// <summary>
        /// スクリプトツリーから国家データを読み込み、診断情報とともに返す。
        /// 診断情報には、ファイルパス情報を含める。
        /// </summary>
        /// <returns>ロード結果</returns>
        internal LoadOutput<Country> Load()
        {
            _diagnostics.Clear();
            var countries = new List<Country>();

            foreach (var topLevelNode in _tree.Root.Children)
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

            return new(countries, _diagnostics);
        }

        // ブロックプロパティノードから国家データを読み込む。必須プロパティが不足している場合はエラー診断を追加し、false を返す。
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
                    case "seal_and_signature_texture":
                        if (TryParseToString(propertyNode, out var sealAndSignatureTexture)) countryBuilder.SealAndSignatureTexture = sealAndSignatureTexture;
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
                        // 予期しないプロパティがあった場合は警告を追加する。
                        // バージョンアップなどで新しいプロパティが追加された場合に、古いバージョンのツールでも読み込みを続行できるようにするため。
                        AddWarning($"Unexpected property \"{propertyNode.Key.Text}\" in country definition. This property will be ignored.", propertyNode.Key.Span, propertyNode.LinePosition);
                        break;
                }
            }

            // 必須プロパティが不足している場合はエラー診断を追加し、false を返す。
            var missings = countryBuilder.GetMissingRequiredProperties();
            if (missings.Count > 0)
            {
                AddError($"Missing required properties ({string.Join(", ", missings)}) for country with tag \"{tag}\": {string.Join(", ", missings)}.", node.Span, node.LinePosition);
                country = default!;
                return false;
            }

            country = countryBuilder.Build();
            return true;
        }

        // PropertyNode から値をパースするためのヘルパーメソッド群
        // PropertyNodeParsers クラスの TryParse メソッドを呼び出し、失敗した場合は診断情報にファイルパスを追加する。
        private bool TryParseToString(PropertyNode node, [NotNullWhen(true)] out string value)
        {
            if (PropertyNodeParsers.TryParseToString(node, out value, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic with { FilePath = FilePath });
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
                _diagnostics.Add(diagnostic with { FilePath = FilePath });
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
                _diagnostics.Add(diagnostic with { FilePath = FilePath });
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
                _diagnostics.Add(diagnostic with { FilePath = FilePath });
                value = default;
                return false;
            }
        }

        private bool TryParseToGameColor(PropertyNode node, out GameColor color)
        {
            // ゲームカラーは、カラーの名前か、RGB値のブロックで指定される。
            if (node is ScalarPropertyNode scalarPropertyNode)
            {
                var colorName = scalarPropertyNode.Value.Token.Text;
                // カラーの名前はダブルクォーテーションで囲まれている場合があるので、必要に応じてトリムする。
                if (colorName[0] == '"' && colorName[^1] == '"')
                {
                    colorName = colorName[1..^1];
                }

                var namedColor = _namedColors.FirstOrDefault(c => c.Name == colorName);
                if (namedColor is null)
                {
                    AddError($"Unknown named color \"{colorName}\".", scalarPropertyNode.Value.Token.Span, scalarPropertyNode.LinePosition);
                    color = default;
                    return false;
                }
                color = namedColor.Color;
                return true;
            }
            else if (PropertyNodeParsers.TryParseToGameColor(node, out color, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic with { FilePath = FilePath });
                color = default;
                return false;
            }
        }

        // エラー診断を追加するためのヘルパーメソッド
        private void AddError(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, message, span, linePosition, FilePath));
        private void AddWarning(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, message, span, linePosition, FilePath));

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
            internal string? SealAndSignatureTexture { get; set; }


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
                    TertiaryUnitColor: TertiaryUnitColor,
                    SealAndSignatureTexture: SealAndSignatureTexture);

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
