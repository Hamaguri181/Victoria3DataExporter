using FluentAssertions;
using PdxScriptAnalysis;
using Victoria3.GameData;
using Victoria3.Loading.Loaders;

namespace Victoria3.Loading.Tests.Loaders
{
    public class CountryLoaderTests
    {
        private const string MinimalCountry = """
        GER = {
            color = { 147 130 110 }
            country_type = recognized
            tier = empire
            cultures = { north_german }
            capital = STATE_BRANDENBURG
        }
        """;

        private static IEnumerable<ScriptTree> ParseTrees(params string[] texts)
            => texts.Select(ScriptTree.ParseText);

        // --- 正常系 ---

        [Fact(DisplayName = "最小構成のデータを読み込むと必須フィールドが正しく読み込まれる")]
        public void Load_MinimalCountry_ParsesRequiredFields()
        {
            var loader = new CountryLoader(ParseTrees(MinimalCountry));
            var output = loader.Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Should().ContainSingle();

            var country = output.Values[0];
            country.Tag.Should().Be("GER");
            country.Color.Should().Be(new GameColor(147, 130, 110));
            country.Type.Should().Be(CountryType.Recognized);
            country.Tier.Should().Be(CountryTier.Empire);
            country.Cultures.Should().Equal("north_german");
            country.Capital.Should().Be("STATE_BRANDENBURG");
        }

        [Fact(DisplayName = "最小構成のデータを読み込むとオプションフィールドはデフォルト値になる")]
        public void Load_MinimalCountry_OptionalFieldsHaveDefaultValues()
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

        [Fact(DisplayName = "すべてのオプションフィールドをロードできる")]
        public void Load_AllOptionalFields_CanBeLoaded()
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
                valid_as_home_country_for_separatists = { foo = bar }
                primary_unit_color = rgb { 10 20 30 }
                secondary_unit_color = hsv { 0.0 0.0 1.0 }
                tertiary_unit_color = hsv360 { 0 0 100 }
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Should().ContainSingle();

            var country = output.Values[0];
            country.SocialHierarchy.Should().Be("monarchy");
            country.Religion.Should().Be("shinto");
            country.IsNamedFromCapital.Should().BeTrue();
            country.ValidAsHomeCountryForSeparatists.Should().NotBeNull();
            country.PrimaryUnitColor.Should().Be(new GameColor(10, 20, 30));
            country.SecondaryUnitColor.Should().Be(new GameColor(255, 255, 255));
            country.TertiaryUnitColor.Should().Be(new GameColor(255, 255, 255));
        }

        [Fact(DisplayName = "1つのスクリプトツリー上の複数データをロードできる")]
        public void Load_MultipleCountriesInSingleTree_CanBeLoaded()
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

            output.Diagnostics.Should().BeEmpty();
            output.Values.Select(x => x.Tag).Should().Equal("GER", "FRA");
        }

        [Fact(DisplayName = "複数のスクリプトツリーのデータをロードできる")]
        public void Load_MultipleTrees_CanBeLoaded()
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

            output.Diagnostics.Should().BeEmpty();
            output.Values.Select(x => x.Tag).Should().Equal("GER", "FRA");
        }

        [Theory(DisplayName = "CountryType のパース（Country固有データ）が正しく動作する")]
        [InlineData("recognized", CountryType.Recognized)]
        [InlineData("colonial", CountryType.Colonial)]
        [InlineData("unrecognized", CountryType.Unrecognized)]
        [InlineData("decentralized", CountryType.Decentralized)]
        public void Load_CountryTypeParsing_Works(string rawType, CountryType expected)
        {
            var input = $$"""
            X = {
                color = { 1 2 3 }
                country_type = {{rawType}}
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;
            var output = new CountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values[0].Type.Should().Be(expected);
        }

        [Theory(DisplayName = "CountryTier のパース（Country固有データ）が正しく動作する")]
        [InlineData("hegemony", CountryTier.Hegemony)]
        [InlineData("empire", CountryTier.Empire)]
        [InlineData("grand-principality", CountryTier.GrandPrincipality)]
        public void Load_CountryTierParsing_Works(string rawTier, CountryTier expected)
        {
            var input = $$"""
            X = {
                color = { 1 2 3 }
                country_type = recognized
                tier = {{rawTier}}
                cultures = { foo }
                capital = STATE_X
            }
            """;
            var output = new CountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values[0].Tier.Should().Be(expected);
        }

        [Fact(DisplayName = "Color のパース（Country固有データ）が正しく動作する")]
        public void Load_ColorParsing_Works()
        {
            var input = """
            X = {
                color = hsv360 { 0 0 100 }
                country_type = recognized
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;
            var output = new CountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values[0].Color.Should().Be(new GameColor(255, 255, 255));
        }

        [Fact(DisplayName = "Load() を再呼び出しすると診断がリセットされる")]
        public void Load_CalledTwice_DiagnosticsAreReset()
        {
            var invalid = """
            X = {
                country_type = recognized
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;
            var loader = new CountryLoader(ParseTrees(invalid));

            var first = loader.Load();
            var second = loader.Load();

            first.Diagnostics.Count(d => d.IsError).Should().Be(1);
            second.Diagnostics.Count(d => d.IsError).Should().Be(1); // 累積しない
        }

        // --- 異常系 ---

        [Theory(DisplayName = "各必須フィールドの欠損でエラーになる")]
        [InlineData("""
            GER = {
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """, "Color")]
        [InlineData("""
            GER = {
                color = { 147 130 110 }
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """, "Type")]
        [InlineData("""
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """, "Tier")]
        [InlineData("""
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                capital = STATE_BRANDENBURG
            }
            """, "Cultures")]
        public void Load_MissingRequiredField_ReturnsError(string input, string missingField)
        {
            var output = new CountryLoader(ParseTrees(input)).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError && d.Message.Contains(missingField));
        }

        [Fact(DisplayName = "トップレベルノードが無効ならエラー")]
        public void Load_InvalidTopLevelNode_ReturnsError()
        {
            var output = new CountryLoader(ParseTrees("some_scalar_value")).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
        }

        [Fact(DisplayName = "ロード可能データと不可能データが混在する場合、可能なデータはロードされエラーも返る")]
        public void Load_MixedValidAndInvalidEntries_LoadsValidAndReturnsError()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            BAD = {
                country_type = recognized
            }
            FRA = {
                color = { 50 100 200 }
                country_type = recognized
                tier = kingdom
                cultures = { french }
                capital = STATE_ILE_DE_FRANCE
            }
            """;
            var output = new CountryLoader(ParseTrees(input)).Load();

            output.Values.Select(x => x.Tag).Should().Equal("GER", "FRA");
            output.Diagnostics.Should().Contain(d => d.IsError);
        }

        [Fact(DisplayName = "不明なプロパティがある場合は警告になる")]
        public void Load_UnknownProperty_ReturnsWarning()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
                unknown_prop = foo
            }
            """;

            var output = new CountryLoader(ParseTrees(input)).Load();

            output.Values.Should().ContainSingle();
            output.Diagnostics.Should().ContainSingle(d => d.IsWarning && d.Message.Contains("unknown_prop"));
        }
    }
}
