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

    public static class CountryTypeExtensions
    {
        /// <summary>
        /// 国家のタイプをローカライズキーに変換する拡張メソッド。
        /// </summary>
        /// <param name="type">変換する国家のタイプ。</param>
        /// <returns>国家のタイプに対応するローカライズキー。</returns>
        public static string ToLocalizationKey(this CountryType type)
            => type switch
            {
                CountryType.Recognized => "recognized",
                CountryType.Colonial => "colonial",
                CountryType.Unrecognized => "unrecognized",
                CountryType.Decentralized => "decentralized",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unexpected country type")
            };
    }
}
