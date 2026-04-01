using FluentAssertions;
using PdxScriptAnalysis;
using Victoria3.GameData;
using Victoria3.Loading.Loaders;

namespace Victoria3.Loading.Tests.Loaders
{
    public class HistoricalStateRegionLoaderTests
    {
        private static IEnumerable<ScriptTree> ParseTrees(params string[] texts)
            => texts.Select(ScriptTree.ParseText);

        [Fact(DisplayName = "最小構成のデータを読み込める")]
        public void Load_MinimalHistoricalStateRegion_CanBeLoaded()
        {
            var input = """
            STATES = {
                STATE_BRANDENBURG = { }
            }
            """;

            var output = new HistoricalStateRegionLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Should().ContainSingle();
            output.Values[0].Tag.Should().Be("STATE_BRANDENBURG");
        }

        [Fact(DisplayName = "create_state を2要素形式でロードできる")]
        public void Load_CreateState_TwoChildren_Works()
        {
            var input = """
            STATES = {
                STATE_BRANDENBURG = {
                    create_state = {
                        country = GER
                        owned_provinces = { x1 x2 }
                    }
                }
            }
            """;

            var output = new HistoricalStateRegionLoader(ParseTrees(input)).Load();
            var createState = output.Values[0].CreateStates.Single();

            output.Diagnostics.Should().BeEmpty();
            createState.Country.Should().Be("GER");
            createState.StateType.Should().BeNull();
            createState.Provinces.Should().Equal("x1", "x2");
        }

        [Fact(DisplayName = "create_state を3要素形式(state_type付き)でロードできる")]
        public void Load_CreateState_WithStateType_Works()
        {
            var input = """
            STATES = {
                STATE_BRANDENBURG = {
                    create_state = {
                        country = GER
                        owned_provinces = { x1 x2 }
                        state_type = incorporated
                    }
                }
            }
            """;

            var output = new HistoricalStateRegionLoader(ParseTrees(input)).Load();
            var createState = output.Values[0].CreateStates.Single();

            output.Diagnostics.Should().BeEmpty();
            createState.Country.Should().Be("GER");
            createState.StateType.Should().Be("incorporated");
            createState.Provinces.Should().Equal("x1", "x2");
        }

        [Fact(DisplayName = "add_homeland と add_claim を複数ロードできる")]
        public void Load_HomelandsAndClaims_Works()
        {
            var input = """
            STATES = {
                STATE_BRANDENBURG = {
                    add_homeland = north_german
                    add_homeland = south_german
                    add_claim = GER
                    add_claim = PRU
                }
            }
            """;

            var output = new HistoricalStateRegionLoader(ParseTrees(input)).Load();
            var r = output.Values[0];

            output.Diagnostics.Should().BeEmpty();
            r.Homelands.Should().Equal("north_german", "south_german");
            r.Claims.Should().Equal("GER", "PRU");
        }

        [Fact(DisplayName = "1つのSTATESブロック内の複数データをロードできる")]
        public void Load_MultipleRegionsInSingleTree_CanBeLoaded()
        {
            var input = """
            STATES = {
                STATE_A = { }
                STATE_B = { }
            }
            """;

            var output = new HistoricalStateRegionLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Select(x => x.Tag).Should().Equal("STATE_A", "STATE_B");
        }

        [Fact(DisplayName = "複数スクリプトツリーのデータをロードできる")]
        public void Load_MultipleTrees_CanBeLoaded()
        {
            var t1 = """
            STATES = {
                STATE_A = { }
            }
            """;
            var t2 = """
            STATES = {
                STATE_B = { }
            }
            """;

            var output = new HistoricalStateRegionLoader(ParseTrees(t1, t2)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Select(x => x.Tag).Should().Equal("STATE_A", "STATE_B");
        }

        [Fact(DisplayName = "Load() の再呼び出しで診断がリセットされる")]
        public void Load_CalledTwice_DiagnosticsAreReset()
        {
            var invalid = """
            STATES = {
                STATE_A = {
                    create_state = {
                        country = GER
                    }
                }
            }
            """;

            var loader = new HistoricalStateRegionLoader(ParseTrees(invalid));

            var first = loader.Load();
            var second = loader.Load();

            first.Diagnostics.Count(d => d.IsError).Should().Be(1);
            second.Diagnostics.Count(d => d.IsError).Should().Be(1);
        }

        [Fact(DisplayName = "トップレベルノード数が不正ならエラー")]
        public void Load_TopLevelNodeCountInvalid_ReturnsError()
        {
            var input = """
            STATES = { }
            OTHER = { }
            """;

            var output = new HistoricalStateRegionLoader(ParseTrees(input)).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
        }

        [Fact(DisplayName = "トップレベルキーがSTATESでなければエラー")]
        public void Load_TopLevelKeyInvalid_ReturnsError()
        {
            var input = """
            FOO = { }
            """;

            var output = new HistoricalStateRegionLoader(ParseTrees(input)).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
        }

        [Fact(DisplayName = "不明なプロパティがある場合は警告になる")]
        public void Load_UnknownProperty_ReturnsWarning()
        {
            var input = """
            STATES = {
                STATE_A = {
                    foo = bar
                }
            }
            """;

            var output = new HistoricalStateRegionLoader(ParseTrees(input)).Load();

            output.Values.Should().ContainSingle();
            output.Diagnostics.Should().ContainSingle(d => d.IsWarning && d.Message.Contains("foo"));
        }
    }
}