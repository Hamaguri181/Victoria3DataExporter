namespace Victoria3.GameData
{
    public sealed record CreateState(
        string Country,
        string? StateType,
        IReadOnlyList<string> Provinces)
    {
        public override string ToString()
            => $"{Provinces.Count} province(s) owned by {Country}";
    }
}
