namespace Victoria3.GameData
{
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
