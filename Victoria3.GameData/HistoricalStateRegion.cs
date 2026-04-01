namespace Victoria3.GameData
{
    /// <summary>
    /// 歴史的州地域を表すレコード。歴史的州地域は、ゲーム開始時点でどの国がどの州を所有しているかを表す。
    /// </summary>
    /// <param name="Tag">州地域のタグ</param>
    /// <param name="CreateStates">州地域に含まれる州のリスト</param>
    /// <param name="Homelands">この州地域を母国とする文化のリスト</param>
    /// <param name="Claims">この州地域に請求権を持つ国のリスト</param>
    public sealed record HistoricalStateRegion(
        string Tag,
        IReadOnlyList<CreateState> CreateStates,
        IReadOnlyList<string> Homelands,
        IReadOnlyList<string> Claims)
        : IPropertySchemaProvider<HistoricalStateRegion>
    {
        private static readonly PropertySchema<HistoricalStateRegion>[] _propertySchemas =
        [
            new PropertySchema<HistoricalStateRegion>(typeof(string), "Tag", c => c.Tag),
            new PropertySchema<HistoricalStateRegion>(typeof(IReadOnlyList<CreateState>), "Create States", c => c.CreateStates),
            new PropertySchema<HistoricalStateRegion>(typeof(IReadOnlyList<string>), "Homelands", c => c.Homelands),
            new PropertySchema<HistoricalStateRegion>(typeof(IReadOnlyList<string>), "Claims", c => c.Claims),
        ];

        /// <inheritdoc/>
        public static PropertySchema<HistoricalStateRegion>[] PropertySchemas
            => _propertySchemas;
    }
}
