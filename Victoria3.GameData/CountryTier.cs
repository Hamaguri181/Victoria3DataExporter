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

    public static class CountryTierExtensions
    {
        /// <summary>
        /// 国家のティアをローカライズキーに変換する拡張メソッド。
        /// </summary>
        /// <param name="tier">変換する国家のティア。</param>
        /// <returns>国家のティアに対応するローカライズキー。</returns>
        /// <exception cref="ArgumentOutOfRangeException">予期しない国家のティアが指定された場合にスローされる。</exception>
        public static string ToLocalizationKey(this CountryTier tier)
            => tier switch
            {
                CountryTier.Hegemony => "country_tier_hegemony",
                CountryTier.Empire => "country_tier_empire",
                CountryTier.Kingdom => "country_tier_kingdom",
                CountryTier.GrandPrincipality => "country_tier_grand_principality",
                CountryTier.Principality => "country_tier_principality",
                CountryTier.CityState => "country_tier_city_state",
                _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unexpected country tier")
            };

        /// <summary>
        /// 国家のティアの表示名を取得する拡張メソッド。
        /// </summary>
        /// <param name="tier">取得する国家のティア。</param>
        /// <returns>国家のティアに対応する表示名。</returns>
        /// <exception cref="ArgumentOutOfRangeException">予期しない国家のティアが指定された場合にスローされる。</exception>
        public static string ToDisplayName(this CountryTier tier)
            => tier switch
            {
                CountryTier.Hegemony => "Hegemony",
                CountryTier.Empire => "Empire",
                CountryTier.Kingdom => "Kingdom",
                CountryTier.GrandPrincipality => "Grand Principality",
                CountryTier.Principality => "Principality",
                CountryTier.CityState => "City State",
                _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unexpected country tier")
            };
    }
}
