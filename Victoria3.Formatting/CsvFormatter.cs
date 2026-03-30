using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Victoria3.Localization;

namespace Victoria3.Formatting
{
    public sealed class CsvFormatter<T> : IGameDataFormatter<T>
    {
        private static readonly CsvColumn[] _columns;

        static CsvFormatter()
        {
            _columns = typeof(T)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetMethod is not null)
                .OrderBy(p => p.MetadataToken)
                .Select(p => new CsvColumn(p.Name, CreateGetter(p)))
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
                        .Select(o => Localize(o?.ToString() ?? string.Empty, localizer));
                    return Escape(string.Join(", ", localizedItems));
                default:
                    var text = value.ToString() ?? string.Empty;
                    return Escape(Localize(text, localizer));
            }
        }

        private static string Localize(string text, ILocalizer? localizer)
            => localizer?.Localize(text) ?? text;

        private static string Escape(string text)
        {
            if (text.Contains(',') || text.Contains('"') || text.Contains('\n') || text.Contains('\r'))
            {
                return $"\"{text.Replace("\"", "\"\"")}\"";
            }

            return text;
        }

        private readonly record struct CsvColumn(string Name, Func<T, object?> Getter);
    }
}
