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
