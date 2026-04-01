namespace Victoria3.GameData
{
    /// <summary>
    /// 解放可能国家を表すレコード。解放可能国家は、特定の条件を満たすことでゲーム内で解放されることができる国家を表す。
    /// </summary>
    /// <param name="Tag">国家のタグ。</param>
    /// <param name="States">必要な州のリスト。</param>
    /// <param name="Provinces">必要なプロヴィンスのリスト。</param>
    /// <param name="UseCultureStates">必要な州として文化に基づく州を使用するかどうか。</param>
    /// <param name="RequiredNumStates">必要な州の数。</param>
    /// <param name="AIWillDo">AIが実行するかどうか。</param>
    /// <param name="Possible">解放の発動条件。</param>
    public sealed record ReleasableCountry(
        string Tag,
        IReadOnlyList<string> States,
        IReadOnlyList<string> Provinces,
        bool UseCultureStates,
        int? RequiredNumStates,
        object? AIWillDo,
        object? Possible)
        : IPropertySchemaProvider<ReleasableCountry>
    {
        private static readonly PropertySchema<ReleasableCountry>[] _propertySchemas =
        [
            new PropertySchema<ReleasableCountry>(typeof(string), "Tag", c => c.Tag),
            new PropertySchema<ReleasableCountry>(typeof(IReadOnlyList<string>), "States", c => c.States),
            new PropertySchema<ReleasableCountry>(typeof(IReadOnlyList<string>), "Provinces", c => c.Provinces),
            new PropertySchema<ReleasableCountry>(typeof(bool), "Use Culture States", c => c.UseCultureStates),
            new PropertySchema<ReleasableCountry>(typeof(int), "Required States Num", c => c.RequiredNumStates),
            new PropertySchema<ReleasableCountry>(typeof(object), "AI Will Do", c => c.AIWillDo),
            new PropertySchema<ReleasableCountry>(typeof(object), "Possible", c => c.Possible),
        ];

        /// <inheritdoc/>
        public static PropertySchema<ReleasableCountry>[] PropertySchemas
            => _propertySchemas;
    }
}
