using System.Collections;
using System.Text;
using Victoria3.GameData;
using Victoria3.Localization;

namespace Victoria3.Formatting
{
    /// <summary>
    /// ゲームデータのコレクションをCSV形式の文字列にフォーマットするクラス。
    /// 対象とするゲームデータの型は、<see cref="IPropertySchemaProvider{T}"/>を実装している必要がある。
    /// </summary>
    /// <typeparam name="T">ゲームデータの型。<see cref="IPropertySchemaProvider{T}"/>を実装している必要がある。</typeparam>
    public static class CsvFormatter<T> where T : IPropertySchemaProvider<T>
    {
        /// <summary>
        /// ゲームデータのコレクションをCSV形式の文字列にフォーマットする。
        /// </summary>
        /// <param name="items">フォーマットするゲームデータのコレクション。</param>
        /// <param name="localizer">ローカライゼーション用のローカライザー。指定しない場合はローカライズされない。</param>
        /// <returns>CSV形式の文字列。</returns>
        public static string Format(IEnumerable<T> items, ILocalizer? localizer = null)
        {
            var sb = new StringBuilder();

            // ヘッダー行
            sb.AppendLine(string.Join(",", T.PropertySchemas.Select(s => Escape(s.DisplayName))));

            foreach (var item in items)
            {
                var row = T.PropertySchemas.Select(s =>
                {
                    var value = s.LocalizationKeyGetter?.Invoke(item) ?? s.Getter(item);
                    return FormatCell(value, localizer);
                });

                sb.AppendLine(string.Join(",", row));
            }
            return sb.ToString();
        }

        // セルの値を文字列に変換し、必要に応じてローカライズしてエスケープする。
        private static string FormatCell(object? value, ILocalizer? localizer)
        {
            switch (value)
            {
                case null:
                    return string.Empty;
                case string str:
                    return Escape(Localize(str, localizer));
                case IEnumerable<string> strings:
                    var localizedStrings = strings.Select(s => Localize(s, localizer));
                    return Escape(string.Join(", ", localizedStrings));
                case IEnumerable enumerable when value is not string:
                    var localizedItems = enumerable
                        .Cast<object?>()
                        .Select(o => o?.ToString() ?? string.Empty);
                    return Escape(string.Join(", ", localizedItems));
                default:
                    var text = value.ToString() ?? string.Empty;
                    return Escape(text);
            }
        }

        private static string Localize(string text, ILocalizer? localizer)
            => localizer?.Localize(text) ?? text;

        // CSVのセルの値をエスケープする。値にカンマ、ダブルクォート、改行が含まれている場合は、ダブルクォートで囲み、ダブルクォート自体は2つにする。
        private static string Escape(string text)
        {
            if (text.Contains(',') || text.Contains('"') || text.Contains('\n') || text.Contains('\r'))
            {
                return $"\"{text.Replace("\"", "\"\"")}\"";
            }

            return text;
        }
    }
}
