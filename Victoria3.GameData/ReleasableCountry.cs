namespace Victoria3.GameData
{
    public sealed record ReleasableCountry(
        string Tag,
        IReadOnlyList<string> States,
        IReadOnlyList<string> Provinces,
        bool UseCultureStates,
        int? RequiredNumStates,
        object? AIWillDo,
        object? Possible)
    {
        public static PropertySchema<ReleasableCountry>[] PropertySchemas =>
        [
            new PropertySchema<ReleasableCountry>(typeof(string), "Tag", c => c.Tag),
            new PropertySchema<ReleasableCountry>(typeof(IReadOnlyList<string>), "States", c => c.States),
            new PropertySchema<ReleasableCountry>(typeof(IReadOnlyList<string>), "Provinces", c => c.Provinces),
            new PropertySchema<ReleasableCountry>(typeof(bool), "Use Culture States", c => c.UseCultureStates),
            new PropertySchema<ReleasableCountry>(typeof(int), "Required States Num", c => c.RequiredNumStates),
            new PropertySchema<ReleasableCountry>(typeof(object), "AI Will Do", c => c.AIWillDo),
            new PropertySchema<ReleasableCountry>(typeof(object), "Possible", c => c.Possible),
        ];
    }
}
