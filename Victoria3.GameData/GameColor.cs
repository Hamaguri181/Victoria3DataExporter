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
        byte B)
    {
        /// <summary>
        /// カラーコードに変換する。形式は "#RRGGBB" となる。
        /// </summary>
        /// <returns>カラーコード文字列</returns>
        public string ToColorCode()
        {
            return $"#{R:X2}{G:X2}{B:X2}";
        }

        public override string ToString()
            => ToColorCode();
    }
}
