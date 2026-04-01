using FluentAssertions;
using PdxScriptAnalysis;
using Victoria3.Loading.Loaders;

namespace Victoria3.Loading.Tests.Loaders
{
    public class ReleasableCountryLoaderTests
    {
        private static IEnumerable<ScriptTree> ParseTrees(params string[] texts)
            => texts.Select(ScriptTree.ParseText);

        [Fact(DisplayName = "必須条件未満(States/Provincesなし・UseCultureStatesなし)ではエラー")]
        public void Load_MinimalWithoutStatesProvincesUseCultureStates_ReturnsError()
        {
            var input = """
            GER = { }
            """;

            var output = new ReleasableCountryLoader(ParseTrees(input)).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError && d.Message.Contains("States or Provinces or UseCultureStates"));
        }

        [Fact(DisplayName = "states があればロードできる")]
        public void Load_WithStates_CanBeLoaded()
        {
            var input = """
            GER = {
                states = { STATE_BRANDENBURG }
            }
            """;

            var output = new ReleasableCountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Should().ContainSingle();
            output.Values[0].States.Should().Equal("STATE_BRANDENBURG");
            output.Values[0].Provinces.Should().BeEmpty();
        }

        [Fact(DisplayName = "provinces があればロードできる")]
        public void Load_WithProvinces_CanBeLoaded()
        {
            var input = """
            GER = {
                provinces = { x12345 x67890 }
            }
            """;

            var output = new ReleasableCountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Should().ContainSingle();
            output.Values[0].Provinces.Should().Equal("x12345", "x67890");
            output.Values[0].States.Should().BeEmpty();
        }

        [Fact(DisplayName = "use_culture_states = yes で states/provinces なしでもロードできる")]
        public void Load_WithUseCultureStatesYes_WithoutStatesOrProvinces_CanBeLoaded()
        {
            var input = """
            GER = {
                use_culture_states = yes
            }
            """;

            var output = new ReleasableCountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Should().ContainSingle();
            output.Values[0].UseCultureStates.Should().BeTrue();
        }

        [Fact(DisplayName = "すべてのオプションフィールドをロードできる")]
        public void Load_AllOptionalFields_CanBeLoaded()
        {
            var input = """
            GER = {
                states = { STATE_BRANDENBURG STATE_SAXONY }
                provinces = { x12345 x67890 }
                use_culture_states = yes
                required_num_states = 2
                ai_will_do = { base = 1 }
                possible = { always = yes }
            }
            """;

            var output = new ReleasableCountryLoader(ParseTrees(input)).Load();
            var r = output.Values[0];

            output.Diagnostics.Should().BeEmpty();
            r.States.Should().Equal("STATE_BRANDENBURG", "STATE_SAXONY");
            r.Provinces.Should().Equal("x12345", "x67890");
            r.UseCultureStates.Should().BeTrue();
            r.RequiredNumStates.Should().Be(2);
            r.AIWillDo.Should().NotBeNull();
            r.Possible.Should().NotBeNull();
        }

        [Fact(DisplayName = "1つのスクリプトツリー上の複数データをロードできる")]
        public void Load_MultipleInSingleTree_CanBeLoaded()
        {
            var input = """
                GER = {
                    states = { STATE_BRANDENBURG }
                }
                FRA = {
                    states = { STATE_ILE_DE_FRANCE }
                }
                """;

            var output = new ReleasableCountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Select(x => x.Tag).Should().Equal("GER", "FRA");
        }

        [Fact(DisplayName = "複数のスクリプトツリーのデータをロードできる")]
        public void Load_MultipleTrees_CanBeLoaded()
        {
            var t1 = """
                GER = {
                    states = { STATE_BRANDENBURG }
                }
                """;
            var t2 = """
                FRA = {
                    states = { STATE_ILE_DE_FRANCE }
                }
                """;

            var output = new FormableCountryLoader(ParseTrees(t1, t2)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Select(x => x.Tag).Should().Equal("GER", "FRA");
        }

        [Fact(DisplayName = "STATES キーでも states としてロードできる")]
        public void Load_StatesUppercase_Works()
        {
            var input = """
            GER = {
                STATES = { STATE_BRANDENBURG }
            }
            """;

            var output = new ReleasableCountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values[0].States.Should().Equal("STATE_BRANDENBURG");
        }

        [Fact(DisplayName = "Load() の再呼び出しで診断がリセットされる")]
        public void Load_CalledTwice_DiagnosticsAreReset()
        {
            var invalid = """
            GER = {
                required_num_states = abc
            }
            """;

            var loader = new ReleasableCountryLoader(ParseTrees(invalid));

            var first = loader.Load();
            var second = loader.Load();

            first.Diagnostics.Count(d => d.IsError).Should().Be(2);
            second.Diagnostics.Count(d => d.IsError).Should().Be(2);
        }

        [Fact(DisplayName = "トップレベルノードが無効ならエラー")]
        public void Load_InvalidTopLevelNode_ReturnsError()
        {
            var output = new ReleasableCountryLoader(ParseTrees("foo")).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
        }

        [Fact(DisplayName = "有効データと不正データが混在してもロードは継続しエラーも返る")]
        public void Load_MixedValidAndInvalidEntries_ReturnsErrorAndContinues()
        {
            var input = """
            GER = { required_num_states = abc }
            FRA = { states = { STATE_ILE_DE_FRANCE } }
            """;

            var output = new ReleasableCountryLoader(ParseTrees(input)).Load();

            output.Values.Should().HaveCount(1);
            output.Diagnostics.Should().HaveCount(2);
            output.Diagnostics.Should().Contain(d => d.IsError);
        }

        [Fact(DisplayName = "不明なプロパティがある場合は警告になる")]
        public void Load_UnknownProperty_ReturnsWarning()
        {
            var input = """
            GER = {
                foo = bar
            }
            """;

            var output = new ReleasableCountryLoader(ParseTrees(input)).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsWarning && d.Message.Contains("foo"));
        }
    }
}