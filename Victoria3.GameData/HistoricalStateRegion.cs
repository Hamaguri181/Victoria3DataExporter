namespace Victoria3.GameData
{
    public sealed record HistoricalStateRegion(
        string Tag,
        IReadOnlyList<CreateState> CreateStates,
        IReadOnlyList<string> Homelands,
        IReadOnlyList<string> Claims)
    {
        public static PropertySchema<HistoricalStateRegion>[] PropertySchemas =>
        [
            new PropertySchema<HistoricalStateRegion>(typeof(string), "Tag", c => c.Tag),
            new PropertySchema<HistoricalStateRegion>(typeof(IReadOnlyList<CreateState>), "Create States", c => c.CreateStates),
            new PropertySchema<HistoricalStateRegion>(typeof(IReadOnlyList<string>), "Homelands", c => c.Homelands),
            new PropertySchema<HistoricalStateRegion>(typeof(IReadOnlyList<string>), "Claims", c => c.Claims),
        ];
    }
}
