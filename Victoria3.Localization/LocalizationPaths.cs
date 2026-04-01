namespace Victoria3.Localization
{
    /// <summary>
    /// 翻訳ファイルのパスを定義するクラス。
    /// </summary>
    public static class LocalizationPaths
    {
        public static string Japanese => @"localization\japanese";
        public static string English => @"localization\english";

        /// <summary>
        /// 言語名を指定して対応する翻訳ファイルのパスを取得するメソッド。
        /// </summary>
        /// <param name="language">取得したい翻訳ファイルの言語名。</param>
        /// <returns>指定された言語に対応する翻訳ファイルのパス。</returns>
        /// <exception cref="ArgumentException">サポートされていない言語名が指定された場合にスローされる。</exception>
        public static string GetPath(string language)
            => language.ToLower() switch
            {
                "japanese" => Japanese,
                "english" => English,
                _ => throw new ArgumentException($"Unsupported language: {language}", nameof(language))
            };
    }
}
