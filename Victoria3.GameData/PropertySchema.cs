namespace Victoria3.GameData
{
    /// <summary>
    /// ゲームデータのプロパティのスキーマを表す構造体。
    /// </summary>
    /// <typeparam name="T">ゲームデータの型。</typeparam>
    /// <param name="Type">プロパティの型情報。</param>
    /// <param name="DisplayName">プロパティの表示名。</param>
    /// <param name="Getter">プロパティの値を取得する関数。</param>
    /// <param name="LocalizationKeyGetter">プロパティのローカライズキーを取得する関数。省略可能。</param>
    public readonly record struct PropertySchema<T>(
        Type Type,
        string DisplayName,
        Func<T, object?> Getter,
        Func<T, string>? LocalizationKeyGetter = null);
}
