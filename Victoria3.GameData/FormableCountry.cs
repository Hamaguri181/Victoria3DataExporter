namespace Victoria3.GameData
{
    /// <summary>
    /// 形成可能国家を表すレコード。形成可能国家は、特定の条件を満たすことでゲーム内で形成されることができる国家を表す。
    /// </summary>
    /// <param name="Tag">国家のタグ。</param>
    /// <param name="States">必要な州のリスト。</param>
    /// <param name="UseCultureStates">必要な州として文化に基づく州を使用するかどうか。</param>
    /// <param name="RequiredStatesFraction">必要な州の割合。</param>
    /// <param name="AIWillDo">AIが実行するかどうか。</param>
    /// <param name="Potential">形成の潜在条件。</param>
    /// <param name="Possible">形成の発動条件。</param>
    /// <param name="GeographicRegion">地理的な地域。</param>
    /// <param name="IsMajorFormation">大国統一かどうか。</param>
    /// <param name="UnificationPlay">統一外交戦の情報。</param>
    /// <param name="LeadershipPlay">リーダーシップ外交戦の情報。</param>
    /// <param name="MaxNumFormationCandidates">統一候補の最大数。</param>
    /// <param name="CanBeFormationCandidate">統一候補になれるかどうか。</param>
    /// <param name="CanBeUnificationTarget">統一の対象になれるかどうか。</param>
    public sealed record FormableCountry(
        string Tag,
        IReadOnlyList<string> States,
        bool UseCultureStates,
        decimal RequiredStatesFraction,
        object? AIWillDo,
        object? Potential,
        object? Possible,
        string? GeographicRegion,
        bool IsMajorFormation,
        string? UnificationPlay,
        string? LeadershipPlay,
        int? MaxNumFormationCandidates,
        object? CanBeFormationCandidate,
        object? CanBeUnificationTarget)
        : IPropertySchemaProvider<FormableCountry>
    {
        private static readonly PropertySchema<FormableCountry>[] _propertySchemas =
        [
            new PropertySchema<FormableCountry>(typeof(string), "Tag", c => c.Tag),
            new PropertySchema<FormableCountry>(typeof(IReadOnlyList<string>), "States", c => c.States),
            new PropertySchema<FormableCountry>(typeof(bool), "Use Culture States", c => c.UseCultureStates),
            new PropertySchema<FormableCountry>(typeof(decimal), "Required States Fraction", c => c.RequiredStatesFraction),
            new PropertySchema<FormableCountry>(typeof(object), "AI Will Do", c => c.AIWillDo),
            new PropertySchema<FormableCountry>(typeof(object), "Potential", c => c.Potential),
            new PropertySchema<FormableCountry>(typeof(object), "Possible", c => c.Possible),
            new PropertySchema<FormableCountry>(typeof(string), "Geographic Region", c => c.GeographicRegion),
            new PropertySchema<FormableCountry>(typeof(bool), "Is Major Formation", c => c.IsMajorFormation),
            new PropertySchema<FormableCountry>(typeof(string), "Unification Play", c => c.UnificationPlay),
            new PropertySchema<FormableCountry>(typeof(string), "Leadership Play", c => c.LeadershipPlay),
            new PropertySchema<FormableCountry>(typeof(decimal), "Max Num Formation Candidates", c => c.MaxNumFormationCandidates),
            new PropertySchema<FormableCountry>(typeof(object), "Can Be Formation Candidate", c => c.CanBeFormationCandidate),
            new PropertySchema<FormableCountry>(typeof(object), "Can Be Unification Target", c => c.CanBeUnificationTarget),
        ];

        /// <inheritdoc/>
        public static PropertySchema<FormableCountry>[] PropertySchemas
            => _propertySchemas;
    }
}
