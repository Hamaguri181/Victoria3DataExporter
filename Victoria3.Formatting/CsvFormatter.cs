using System.Collections;
using System.Text;
using Victoria3.GameData;
using Victoria3.Localization;

namespace Victoria3.Formatting
{
    public sealed class CsvFormatter<T>(
        IEnumerable<PropertySchema<T>>? propertySchemas = null
        ) : IGameDataFormatter<T>
    {
        private readonly PropertySchema<T>[] _propertySchemas = propertySchemas?.ToArray() ?? [];


        public string Format(IEnumerable<T> items, ILocalizer? localizer = null)
        {
            var sb = new StringBuilder();

            // ヘッダー行を出力
            sb.AppendLine(string.Join(",", _propertySchemas.Select(s => Escape(s.DisplayName))));

            foreach (var item in items)
            {
                var row = _propertySchemas.Select(s =>
                {
                    var value = s.LocalizationKeyGetter?.Invoke(item) ?? s.Getter(item);
                    return FormatCell(value, localizer);
                });

                sb.AppendLine(string.Join(",", row));
            }
            return sb.ToString();
        }

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
            => localizer is not null ? localizer.Localize(text) : text;

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
