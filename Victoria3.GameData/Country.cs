namespace Victoria3.GameData
{
    /// <summary>
    /// 国家を表すレコード。
    /// </summary>
    /// <param name="Tag">国家のタグ。</param>
    /// <param name="Color">国家の色。</param>
    /// <param name="Type">国家のタイプ。</param>
    /// <param name="Tier">国家のティア。</param>
    /// <param name="SocialHierarchy">国家の社会階層。</param>
    /// <param name="Religion">国家の宗教。</param>
    /// <param name="Cultures">国家の文化。</param>
    /// <param name="Capital">国家の首都。</param>
    /// <param name="IsNamedFromCapital">首都から名前が付けられているかどうか。</param>
    /// <param name="ValidAsHomeCountryForSeparatists">分離主義者の本国として有効かどうか。</param>
    /// <param name="PrimaryUnitColor">主要ユニットの色。</param>
    /// <param name="SecondaryUnitColor">二次ユニットの色。</param>
    /// <param name="TertiaryUnitColor">三次ユニットの色。</param>
    public sealed record Country(
        string Tag,
        GameColor Color,
        CountryType Type,
        CountryTier Tier,
        string? SocialHierarchy,
        string? Religion,
        IReadOnlyList<string> Cultures,
        string? Capital,
        bool IsNamedFromCapital,
        object? ValidAsHomeCountryForSeparatists,
        GameColor? PrimaryUnitColor,
        GameColor? SecondaryUnitColor,
        GameColor? TertiaryUnitColor)
        : IPropertySchemaProvider<Country>
    {
        private static readonly PropertySchema<Country>[] _propertySchemas =
        [
            new PropertySchema<Country>(typeof(string), "Tag", c => c.Tag),
            new PropertySchema<Country>(typeof(GameColor), "Color", c => c.Color),
            new PropertySchema<Country>(typeof(CountryType), "Type", c => c.Type, c => c.Type.ToLocalizationKey()),
            new PropertySchema<Country>(typeof(CountryTier), "Tier", c => c.Tier, c => c.Tier.ToLocalizationKey()),
            new PropertySchema<Country>(typeof(string), "Social Hierarchy", c => c.SocialHierarchy),
            new PropertySchema<Country>(typeof(string), "Religion", c => c.Religion),
            new PropertySchema<Country>(typeof(IReadOnlyList<string>), "Cultures", c => c.Cultures),
            new PropertySchema<Country>(typeof(string), "Capital", c => c.Capital),
            new PropertySchema<Country>(typeof(bool), "Is Named From Capital", c => c.IsNamedFromCapital),
            new PropertySchema<Country>(typeof(object), "Valid As Home Country For Separatists", c => c.ValidAsHomeCountryForSeparatists),
            new PropertySchema<Country>(typeof(GameColor?), "Primary Unit Color", c => c.PrimaryUnitColor),
            new PropertySchema<Country>(typeof(GameColor?), "Secondary Unit Color", c => c.SecondaryUnitColor),
            new PropertySchema<Country>(typeof(GameColor?), "Tertiary Unit Color", c => c.TertiaryUnitColor),
        ];

        /// <inheritdoc/>
        public static PropertySchema<Country>[] PropertySchemas
            => _propertySchemas;
    }
}
