namespace Victoria3.GameData
{
    /// <summary>
    /// 名前付きカラーを表すレコード。
    /// </summary>
    /// <param name="Name">カラー名</param>
    /// <param name="Color">カラー値</param>
    public sealed record NamedColor(
        string Name,
        GameColor Color)
        : IPropertySchemaProvider<NamedColor>
    {
        private static readonly PropertySchema<NamedColor>[] _propertySchemas =
        [
            new PropertySchema<NamedColor>(typeof(string), "Name", c => c.Name),
            new PropertySchema<NamedColor>(typeof(GameColor), "Color", c => c.Color)
        ];
        /// <inheritdoc/>
        public static PropertySchema<NamedColor>[] PropertySchemas
            => _propertySchemas;
    }
}
