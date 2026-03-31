namespace Victoria3.GameData
{
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
        object? CanBeUnificationTarget
        )
    {

        public static PropertySchema<FormableCountry>[] PropertySchemas =>
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
    }
}
