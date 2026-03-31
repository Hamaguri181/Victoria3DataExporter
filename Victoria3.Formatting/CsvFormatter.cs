using System.Collections;
using System.Collections.Frozen;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Victoria3.Localization;

namespace Victoria3.Formatting
{
    public sealed class CsvFormatter<T>(
        IReadOnlyDictionary<Type, Func<object, string>>? localizationKeySelectors = null
        ) : IGameDataFormatter<T>
    {
        // Tのプロパティを表す列の情報を保持する静的な配列。プロパティの型、名前、値を取得する関数を持つ。
        private static readonly CsvColumn[] _columns;
        // ローカライズのキーを取得する関数の辞書。キーはプロパティの型、値はその型の値からローカライズキーを取得する関数。
        private readonly FrozenDictionary<Type, Func<object, string>> _localizationKeySelectors
            = localizationKeySelectors?.ToFrozenDictionary() ?? FrozenDictionary<Type, Func<object, string>>.Empty;

        static CsvFormatter()
        {
            _columns = typeof(T)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetMethod is not null)
                .OrderBy(p => p.MetadataToken)
                .Select(p => new CsvColumn(p.PropertyType, p.Name, CreateGetter(p)))
                .ToArray();
        }

        // 式木を使用してプロパティの値を取得する関数を生成
        private static Func<T, object?> CreateGetter(PropertyInfo property)
        {
            var instance = Expression.Parameter(typeof(T), "gameData");
            var propertyAccess = Expression.Property(instance, property);
            var boxed = Expression.Convert(propertyAccess, typeof(object));
            return Expression.Lambda<Func<T, object?>>(boxed, instance).Compile();
        }


        public string Format(IEnumerable<T> items, ILocalizer? localizer = null)
        {
            var sb = new StringBuilder();

            // ヘッダー行を出力
            sb.AppendLine(string.Join(",", _columns.Select(c => Escape(c.Name))));

            foreach (var item in items)
            {
                var row = _columns.Select(c =>
                {
                    var value = c.Getter(item);
                    return FormatCell(value, c.Type, localizer);
                });

                sb.AppendLine(string.Join(",", row));
            }
            return sb.ToString();
        }

        private string FormatCell(object? value, Type type, ILocalizer? localizer)
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
                case Enum _ when _localizationKeySelectors.TryGetValue(type, out var keySelector):
                    var localizationKey = keySelector(value);
                    return Escape(Localize(localizationKey, localizer));
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

        // CSVの列を表すレコード。列の型、列名、値を取得する関数を持つ。
        private readonly record struct CsvColumn(Type Type, string Name, Func<T, object?> Getter);
    }
}
