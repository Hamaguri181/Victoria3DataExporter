using FluentAssertions;
using PdxScriptAnalysis;
using Victoria3.Loading.Loaders;

namespace Victoria3.Loading.Tests.Loaders
{
    public class FormableCountryLoaderTests
    {
        private static IEnumerable<ScriptTree> ParseTrees(params string[] texts)
            => texts.Select(ScriptTree.ParseText);

        [Fact(DisplayName = "必須条件未満(Statesなし・UseCultureStatesなし)ではエラー")]
        public void Load_MinimalWithoutStatesOrUseCultureStates_ReturnsError()
        {
            var input = """
            GER = { }
            """;

            var output = new FormableCountryLoader(ParseTrees(input)).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError && d.Message.Contains("States or UseCultureStates"));
        }

        [Fact(DisplayName = "states があればロードできる")]
        public void Load_WithStates_CanBeLoaded()
        {
            var input = """
            GER = {
                states = { STATE_BRANDENBURG }
            }
            """;

            var output = new FormableCountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Should().ContainSingle();
            output.Values[0].Tag.Should().Be("GER");
            output.Values[0].States.Should().Equal("STATE_BRANDENBURG");
            output.Values[0].UseCultureStates.Should().BeFalse();
            output.Values[0].RequiredStatesFraction.Should().Be(1m);
        }

        [Fact(DisplayName = "use_culture_states = yes で states なしでもロードできる")]
        public void Load_WithUseCultureStatesYes_WithoutStates_CanBeLoaded()
        {
            var input = """
            GER = {
                use_culture_states = yes
            }
            """;

            var output = new FormableCountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Should().ContainSingle();
            output.Values[0].UseCultureStates.Should().BeTrue();
            output.Values[0].States.Should().BeEmpty();
        }

        [Fact(DisplayName = "is_major_formation = yes で条件付き必須が不足するとエラー")]
        public void Load_MajorFormationMissingConditionalRequired_ReturnsError()
        {
            var input = """
            GER = {
                states = { STATE_BRANDENBURG }
                is_major_formation = yes
            }
            """;

            var output = new FormableCountryLoader(ParseTrees(input)).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("UnificationPlay"));
            output.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("LeadershipPlay"));
            output.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("MaxNumFormationCandidates"));
            output.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("CanBeFormationCandidate"));
        }

        [Fact(DisplayName = "is_major_formation = yes で条件付き必須を満たせばロードできる")]
        public void Load_MajorFormationWithAllRequired_CanBeLoaded()
        {
            var input = """
            GER = {
                states = { STATE_BRANDENBURG }
                is_major_formation = yes
                unification_play = german_unification
                leadership_play = german_leadership
                max_num_formation_candidates = 3
                can_be_formation_candidate = { always = yes }
            }
            """;

            var output = new FormableCountryLoader(ParseTrees(input)).Load();
            var value = output.Values[0];

            output.Diagnostics.Should().BeEmpty();
            output.Values.Should().ContainSingle();
            value.IsMajorFormation.Should().BeTrue();
            value.UnificationPlay.Should().Be("german_unification");
            value.LeadershipPlay.Should().Be("german_leadership");
            value.MaxNumFormationCandidates.Should().Be(3);
            value.CanBeFormationCandidate.Should().NotBeNull();
        }

        [Fact(DisplayName = "すべてのオプションフィールドをロードできる")]
        public void Load_AllOptionalFields_CanBeLoaded()
        {
            var input = """
            GER = {
                states = { STATE_BRANDENBURG STATE_SAXONY }
                use_culture_states = yes
                required_states_fraction = 0.5
                ai_will_do = { base = 1 }
                potential = { always = yes }
                possible = { always = yes }
                geographic_region = central_europe
                is_major_formation = yes
                unification_play = german_unification
                leadership_play = german_leadership
                max_num_formation_candidates = 3
                can_be_formation_candidate = { always = yes }
                can_be_unification_target = { always = yes }
            }
            """;

            var output = new FormableCountryLoader(ParseTrees(input)).Load();
            var f = output.Values[0];

            output.Diagnostics.Should().BeEmpty();
            f.Tag.Should().Be("GER");
            f.States.Should().Equal("STATE_BRANDENBURG", "STATE_SAXONY");
            f.UseCultureStates.Should().BeTrue();
            f.RequiredStatesFraction.Should().Be(0.5m);
            f.AIWillDo.Should().NotBeNull();
            f.Potential.Should().NotBeNull();
            f.Possible.Should().NotBeNull();
            f.GeographicRegion.Should().Be("central_europe");
            f.IsMajorFormation.Should().BeTrue();
            f.UnificationPlay.Should().Be("german_unification");
            f.LeadershipPlay.Should().Be("german_leadership");
            f.MaxNumFormationCandidates.Should().Be(3);
            f.CanBeFormationCandidate.Should().NotBeNull();
            f.CanBeUnificationTarget.Should().NotBeNull();
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

            var output = new FormableCountryLoader(ParseTrees(input)).Load();

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

            var output = new FormableCountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values[0].States.Should().Equal("STATE_BRANDENBURG");
        }

        [Fact(DisplayName = "Load() の再呼び出しで診断がリセットされる")]
        public void Load_CalledTwice_DiagnosticsAreReset()
        {
            var invalid = """
            GER = {
                required_states_fraction = abc
            }
            """;

            var loader = new FormableCountryLoader(ParseTrees(invalid));

            var first = loader.Load();
            var second = loader.Load();

            first.Diagnostics.Count(d => d.IsError).Should().Be(2);
            second.Diagnostics.Count(d => d.IsError).Should().Be(2);
        }

        [Fact(DisplayName = "トップレベルノードが無効ならエラー")]
        public void Load_InvalidTopLevelNode_ReturnsError()
        {
            var output = new FormableCountryLoader(ParseTrees("foo")).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
        }

        [Fact(DisplayName = "有効データと不正データが混在してもロードは継続しエラーも返る")]
        public void Load_MixedValidAndInvalidEntries_ReturnsErrorAndContinues()
        {
            var input = """
            GER = { required_states_fraction = abc }
            FRA = { states = { STATE_ILE_DE_FRANCE } }
            """;

            var output = new FormableCountryLoader(ParseTrees(input)).Load();

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

            var output = new FormableCountryLoader(ParseTrees(input)).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsWarning && d.Message.Contains("foo"));
        }
    }
}