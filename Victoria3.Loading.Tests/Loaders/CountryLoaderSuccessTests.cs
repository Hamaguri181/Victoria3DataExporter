using FluentAssertions;
using PdxScriptAnalysis;
using Victoria3.GameData;
using Victoria3.Loading.Loaders;

namespace Victoria3.Loading.Tests.Loaders
{
    public class CountryLoaderSuccessTests
    {
        // 最小構成の国家定義（必須フィールドのみ）
        private const string MinimalCountry = """
        GER = {
            color = { 147 130 110 }
            country_type = recognized
            tier = empire
            cultures = { north_german }
            capital = STATE_BRANDENBURG
        }
        """;

        // テスト用の ScriptTree を生成するヘルパーメソッド
        private static ScriptTree ParseTree(string text)
            => ScriptTree.ParseText(text);

        // 複数の ScriptTree を生成するヘルパーメソッド
        private static IEnumerable<ScriptTree> ParseTrees(params string[] texts)
            => texts.Select(ScriptTree.ParseText);



        // 正常系

        [Fact(DisplayName = "最小構成の国家データを読み込むと、正しいタグを持つ国家が返される")]
        public void Load_MinimalCountry_ReturnsCountryWithCorrectTag()
        {
            var loader = new CountryLoader(ParseTrees(MinimalCountry));
            var output = loader.Load();

            output.Values.Should().HaveCount(1);
            output.Values[0].Tag.Should().Be("GER");
        }

        [Fact(DisplayName = "最小構成の国家データを読み込むと、エラーが発生しない")]
        public void Load_MinimalCountry_ReturnsNoErrors()
        {
            var loader = new CountryLoader(ParseTrees(MinimalCountry));
            var output = loader.Load();

            output.Diagnostics.Should().BeEmpty();
        }

        [Fact(DisplayName = "最小構成の国家データを読み込むと、必須フィールドが正しく解析される")]
        public void Load_MinimalCountry_ParsesRequiredFields()
        {
            var loader = new CountryLoader(ParseTrees(MinimalCountry));
            var output = loader.Load();

            var country = output.Values[0];
            country.Type.Should().Be(CountryType.Recognized);
            country.Tier.Should().Be(CountryTier.Empire);
            country.Cultures.Should().Equal("north_german");
            country.Capital.Should().Be("STATE_BRANDENBURG");
        }

        [Fact(DisplayName = "最小構成の国家データを読み込むと、オプションフィールドがデフォルト値を持つ")]
        public void Load_MinimalCountry_OptionalFieldsHaveDefaults()
        {
            var loader = new CountryLoader(ParseTrees(MinimalCountry));
            var output = loader.Load();

            var country = output.Values[0];
            country.SocialHierarchy.Should().BeNull();
            country.Religion.Should().BeNull();
            country.IsNamedFromCapital.Should().BeFalse();
            country.ValidAsHomeCountryForSeparatists.Should().BeNull();
            country.PrimaryUnitColor.Should().BeNull();
            country.SecondaryUnitColor.Should().BeNull();
            country.TertiaryUnitColor.Should().BeNull();
        }

        [Fact(DisplayName = "すべてのオプションフィールドが正しく解析される")]
        public void Load_AllOptionalFields_ParsesCorrectly()
        {
            var input = """
            JPN = {
                color = { 255 0 0 }
                country_type = recognized
                tier = empire
                social_hierarchy = monarchy
                religion = shinto
                cultures = { japanese }
                capital = STATE_KANTO
                is_named_from_capital = yes
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            var country = output.Values[0];
            country.SocialHierarchy.Should().Be("monarchy");
            country.Religion.Should().Be("shinto");
            country.IsNamedFromCapital.Should().BeTrue();
        }

        [Fact(DisplayName = "複数の文化を持つ国家データを読み込むと、すべての文化が正しく解析される")]
        public void Load_MultipleCultures_ParsesAllCultures()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                cultures = { north_german south_german }
                capital = STATE_BRANDENBURG
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values[0].Cultures.Should().Equal("north_german", "south_german");
        }

        [Fact(DisplayName = "1つのスクリプトツリーに複数の国家データが含まれる場合、すべての国家が返される")]
        public void Load_MultipleCountriesInOneTree_ReturnsAll()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            FRA = {
                color = { 50 100 200 }
                country_type = recognized
                tier = kingdom
                cultures = { french }
                capital = STATE_ILE_DE_FRANCE
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().HaveCount(2);
            output.Values.Select(c => c.Tag).Should().Equal("GER", "FRA");
        }

        [Fact(DisplayName = "複数のスクリプトツリーに複数の国家データが含まれる場合、すべての国家が返される")]
        public void Load_MultipleScriptTrees_ReturnsAllCountries()
        {
            var tree1 = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """;
            var tree2 = """
            FRA = {
                color = { 50 100 200 }
                country_type = recognized
                tier = kingdom
                cultures = { french }
                capital = STATE_ILE_DE_FRANCE
            }
            """;

            var loader = new CountryLoader(ParseTrees(tree1, tree2));
            var output = loader.Load();

            output.Values.Should().HaveCount(2);
            output.Values.Select(c => c.Tag).Should().Equal("GER", "FRA");
        }

        // --- CountryType のパース ---

        [Theory(DisplayName = "すべての CountryType が正しく解析される")]
        [InlineData("recognized", CountryType.Recognized)]
        [InlineData("colonial", CountryType.Colonial)]
        [InlineData("unrecognized", CountryType.Unrecognized)]
        [InlineData("decentralized", CountryType.Decentralized)]
        public void Load_AllCountryTypes_ParsesCorrectly(string typeText, CountryType expected)
        {
            var input = $$"""
            X = {
                color = { 0 0 0 }
                country_type = {{typeText}}
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values[0].Type.Should().Be(expected);
        }

        // --- CountryTier のパース ---

        [Theory(DisplayName = "すべての CountryTier が正しく解析される")]
        [InlineData("hegemony", CountryTier.Hegemony)]
        [InlineData("empire", CountryTier.Empire)]
        [InlineData("kingdom", CountryTier.Kingdom)]
        [InlineData("grand_principality", CountryTier.GrandPrincipality)]
        [InlineData("principality", CountryTier.Principality)]
        [InlineData("city_state", CountryTier.CityState)]
        public void Load_AllCountryTiers_ParsesCorrectly(string tierText, CountryTier expected)
        {
            var input = $$"""
            X = {
                color = { 0 0 0 }
                country_type = recognized
                tier = {{tierText}}
                cultures = { foo }
                capital = STATE_X
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values[0].Tier.Should().Be(expected);
        }

        // --- 色形式のパース ---

        [Fact(DisplayName = "RGB ブロック形式の色が正しく解析される")]
        public void Load_ColorRgbBlock_ParsesCorrectly()
        {
            var input = """
            X = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values[0].Color.Should().Be(new GameColor(147, 130, 110));
        }

        [Fact(DisplayName = "RGB 型ブロック形式の色が正しく解析される")]
        public void Load_ColorRgbTypedBlock_ParsesCorrectly()
        {
            var input = """
            X = {
                color = rgb { 147 130 110 }
                country_type = recognized
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values[0].Color.Should().Be(new GameColor(147, 130, 110));
        }

        [Fact(DisplayName = "HSV 型ブロック形式の色が正しく解析される")]
        public void Load_ColorHsv_ConvertsToRgb()
        {
            // hsv { 0.0 0.0 1.0 } = 白 (255, 255, 255)
            var input = """
            X = {
                color = hsv { 0.0 0.0 1.0 }
                country_type = recognized
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values[0].Color.Should().Be(new GameColor(255, 255, 255));
        }

        [Fact(DisplayName = "HSV360 型ブロック形式の色が正しく解析される")]
        public void Load_ColorHsv360_ConvertsToRgb()
        {
            // hsv360 { 0 0 100 } = 白 (255, 255, 255)
            var input = """
            X = {
                color = hsv360 { 0 0 100 }
                country_type = recognized
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values[0].Color.Should().Be(new GameColor(255, 255, 255));
        }

        // --- dynamic_country_definition ---

        [Fact(DisplayName = "dynamic_country_definition が yes の場合、国家データはスキップされる")]
        public void Load_DynamicCountryDefinition_IsSkipped()
        {
            var input = """
            DYN = {
                dynamic_country_definition = yes
                color = { 0 0 0 }
                country_type = recognized
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().BeEmpty();
        }

        [Fact(DisplayName = "dynamic_country_definition が no の場合、国家データはスキップされない")]
        public void Load_DynamicCountryDefinitionNo_IsNotSkipped()
        {
            var input = """
            X = {
                dynamic_country_definition = no
                color = { 0 0 0 }
                country_type = recognized
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().HaveCount(1);
        }

        // --- Load() の再呼び出し ---

        [Fact(DisplayName = "Load() を再呼び出しした場合、診断がリセットされる")]
        public void Load_CalledTwice_DiagnosticsAreReset()
        {
            var input = """
            X = {
                color = { 0 0 0 }
                country_type = recognized
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            loader.Load();
            var output = loader.Load();

            // 2回目も同じ結果になること（診断がクリアされること）
            output.Values.Should().HaveCount(1);
            output.Diagnostics.Should().BeEmpty();
        }
    }
}
