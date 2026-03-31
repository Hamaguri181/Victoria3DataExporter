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
        /// 国家の種別をローカライズキーに変換する拡張メソッド。
        /// </summary>
        /// <param name="type">変換する国家のタイプ。</param>
        /// <returns>国家のタイプに対応するローカライズキー。</returns>
        /// <exception cref="ArgumentOutOfRangeException">予期しない国家のタイプが指定された場合にスローされる。</exception>
        public static string ToLocalizationKey(this CountryType type)
            => type switch
            {
                CountryType.Recognized => "recognized",
                CountryType.Colonial => "colonial",
                CountryType.Unrecognized => "unrecognized",
                CountryType.Decentralized => "decentralized",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unexpected country type")
            };

        /// <summary>
        /// 国家の種別の表示名を取得する拡張メソッド。
        /// </summary>
        /// <param name="type">取得する国家のタイプ。</param>
        /// <returns>国家のタイプに対応する表示名。</returns>
        /// <exception cref="ArgumentOutOfRangeException">予期しない国家のタイプが指定された場合にスローされる。</exception>
        public static string ToDisplayName(this CountryType type)
            => type switch
            {
                CountryType.Recognized => "Recognized",
                CountryType.Colonial => "Colonial",
                CountryType.Unrecognized => "Unrecognized",
                CountryType.Decentralized => "Decentralized",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unexpected country type")
            };
    }
}
