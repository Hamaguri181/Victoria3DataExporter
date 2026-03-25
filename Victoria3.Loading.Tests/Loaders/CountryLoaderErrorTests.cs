using FluentAssertions;
using PdxScriptAnalysis;
using Victoria3.Loading.Loaders;

namespace Victoria3.Loading.Tests.Loaders
{
    public class CountryLoaderErrorTests
    {

        // テスト用の ScriptTree を生成するヘルパーメソッド
        private static ScriptTree ParseTree(string text)
            => ScriptTree.ParseText(text);

        // 複数の ScriptTree を生成するヘルパーメソッド
        private static IEnumerable<ScriptTree> ParseTrees(params string[] texts)
            => texts.Select(ScriptTree.ParseText);


        // --- 必須フィールド欠損 ---

        [Fact(DisplayName = "Color が欠損している場合、エラーが返される")]
        public void Load_MissingColor_ReturnsErrorAndNoCountry()
        {
            var input = """
            GER = {
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
            output.Diagnostics[0].Message.Should().Contain("Color");
        }

        [Fact(DisplayName = "Type が欠損している場合、エラーが返される")]
        public void Load_MissingType_ReturnsErrorAndNoCountry()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
            output.Diagnostics[0].Message.Should().Contain("Type");
        }

        [Fact(DisplayName = "Tier が欠損している場合、エラーが返される")]
        public void Load_MissingTier_ReturnsErrorAndNoCountry()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
            output.Diagnostics[0].Message.Should().Contain("Tier");
        }

        [Fact(DisplayName = "Cultures が欠損している場合、エラーが返される")]
        public void Load_MissingCultures_ReturnsErrorAndNoCountry()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                capital = STATE_BRANDENBURG
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
            output.Diagnostics[0].Message.Should().Contain("Cultures");
        }

        [Fact(DisplayName = "複数の必須フィールドが欠損している場合、エラーメッセージにすべての欠損フィールドが含まれる")]
        public void Load_MultipleRequiredFieldsMissing_ErrorMessageContainsAllMissingFields()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);

            var message = output.Diagnostics[0].Message;
            message.Should().Contain("Tier");
            message.Should().Contain("Cultures");
        }

        // --- 不正な値 ---

        [Fact(DisplayName = "不正な CountryType が指定されている場合、エラーが返される")]
        public void Load_UnknownCountryType_ReturnsError()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = republic
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("republic"));
        }

        [Fact(DisplayName = "不正な CountryTier が指定されている場合、エラーが返される")]
        public void Load_UnknownTier_ReturnsError()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = duchy
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("duchy"));
        }

        [Fact(DisplayName = "不正な is_named_from_capital が指定されている場合、エラーが返される")]
        public void Load_InvalidIsNamedFromCapital_ReturnsError()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
                is_named_from_capital = true
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            // is_named_from_capital のエラーが記録されること
            output.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("is_named_from_capital"));
        }

        // --- 色ブロックのエラー ---

        [Fact(DisplayName = "Color ブロックの要素数が不正な場合、エラーが返される")]
        public void Load_ColorBlockWithWrongElementCount_ReturnsError()
        {
            var input = """
            GER = {
                color = { 147 130 }
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("color"));
        }

        [Fact(DisplayName = "Color ブロックに数値以外の値が含まれている場合、エラーが返される")]
        public void Load_ColorBlockWithNonNumericValue_ReturnsError()
        {
            var input = """
            GER = {
                color = { 147 abc 110 }
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("color"));
        }

        [Fact(DisplayName = "不正な Color クオリファイアが指定されている場合、エラーが返される")]
        public void Load_UnknownColorQualifier_ReturnsError()
        {
            var input = """
            GER = {
                color = hsl { 0.5 0.8 0.9 }
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("hsl"));
        }

        // --- トップレベルノードのエラー ---

        [Fact(DisplayName = "トップレベルノードがスカラー値の場合、エラーが返される")]
        public void Load_TopLevelScalarNode_ReturnsError()
        {
            var input = "some_scalar_value";

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
        }

        // --- 失敗エントリと成功エントリの混在 ---

        [Fact(DisplayName = "有効な国と無効な国が混在している場合、有効な国のみが返される")]
        public void Load_ValidAndInvalidCountries_ReturnsOnlyValidCountries()
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

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().HaveCount(2);
            output.Values.Select(c => c.Tag).Should().Equal("GER", "FRA");
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
        }
    }

    public class CountryLoaderWarningTests
    {

        // テスト用の ScriptTree を生成するヘルパーメソッド
        private static ScriptTree ParseTree(string text)
            => ScriptTree.ParseText(text);

        // 複数の ScriptTree を生成するヘルパーメソッド
        private static IEnumerable<ScriptTree> ParseTrees(params string[] texts)
            => texts.Select(ScriptTree.ParseText);


        [Fact(DisplayName = "不明なプロパティが指定されている場合、警告が返される")]
        public void Load_UnknownProperty_ReturnsWarningAndLoadsCountry()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
                flag_color = red
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            // ロードは成功する
            output.Values.Should().HaveCount(1);
            output.Values[0].Tag.Should().Be("GER");

            // 警告が1件記録される
            output.Diagnostics.Should().ContainSingle(d => d.IsWarning);
            output.Diagnostics[0].Message.Should().Contain("flag_color");
        }

        [Fact(DisplayName = "複数の不明なプロパティが指定されている場合、各プロパティに対して警告が返される")]
        public void Load_MultipleUnknownProperties_ReturnsWarningsForEach()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
                flag_color = red
                unknown_prop = foo
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().HaveCount(1);
            output.Diagnostics.Should().HaveCount(2);
            output.Diagnostics.Should().AllSatisfy(d => d.IsWarning.Should().BeTrue());
        }
    }
}
