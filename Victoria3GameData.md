```Victoria3.GameData\Country.cs
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
        string Capital,
        bool IsNamedFromCapital,
        object? ValidAsHomeCountryForSeparatists,
        GameColor? PrimaryUnitColor,
        GameColor? SecondaryUnitColor,
        GameColor? TertiaryUnitColor);
}
```

```Victoria3.GameData\CountryTier.cs
namespace Victoria3.GameData
{
    /// <summary>
    /// 国家のティアを表す列挙型。
    /// </summary>
    public enum CountryTier
    {
        Hegemony,
        Empire,
        Kingdom,
        GrandPrincipality,
        Principality,
        CityState,
    }
}
```

```Victoria3.GameData\CountryType.cs
namespace Victoria3.GameData
{
    /// <summary>
    /// 国家のタイプを表す列挙型。
    /// </summary>
    public enum CountryType
    {
        Recognized,
        Colonial,
        Unrecognized,
        Decentralized,
    }
}
```

```Victoria3.GameData\GameColor.cs
namespace Victoria3.GameData
{
    /// <summary>
    /// ゲーム内で使用される色を表す構造体。
    /// </summary>
    /// <param name="R">赤成分 (0-255)</param>
    /// <param name="G">緑成分 (0-255)</param>
    /// <param name="B">青成分 (0-255)</param>
    public readonly record struct GameColor(
        byte R,
        byte G,
        byte B);
}
```

