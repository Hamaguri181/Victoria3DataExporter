namespace Victoria3.GameData
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Country"></param>
    /// <param name="StateType"></param>
    /// <param name="Provinces"></param>
    public sealed record CreateState(
        string Country,
        string? StateType,
        IReadOnlyList<string> Provinces)
    {
        public override string ToString()
            => $"{Country}({Provinces.Count})";
    }
}
