```Victoria3.Localization\FileLocalizer.cs
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Victoria3.Localization.Parsing;

namespace Victoria3.Localization
{
    /// <summary>
    /// 指定されたローカライズファイルに基づいて文字列をローカライズするクラス。
    /// </summary>
    public class FileLocalizer : ILocalizer
    {
        private readonly FrozenDictionary<string, string> _localizations;

        private FileLocalizer(IReadOnlyDictionary<string, string> localizations)
            => _localizations = localizations.ToFrozenDictionary();


        /// <summary>
        /// 指定されたローカライズデータを使用してFileLocalizerを作成する。
        /// </summary>
        /// <param name="localizations">ローカライズデータの辞書。</param>
        /// <returns>作成されたFileLocalizerインスタンス。</returns>
        /// <exception cref="ArgumentNullException">ローカライズデータがnullの場合にスローされる。</exception>
        public static FileLocalizer FromLocalizations(IReadOnlyDictionary<string, string> localizations)
        {
            ArgumentNullException.ThrowIfNull(localizations);
            return new(localizations);
        }

        /// <summary>
        /// 指定されたテキストを解析してFileLocalizerを作成する。
        /// </summary>
        /// <param name="text">ローカライズデータを含むテキスト。</param>
        /// <returns>作成されたFileLocalizerインスタンス。</returns>
        /// <exception cref="ArgumentNullException">テキストがnullの場合にスローされる。</exception>
        public static FileLocalizer FromText(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            var localizations = LocalizationParser.ParseText(text);
            return new(localizations);
        }

        /// <summary>
        /// 指定されたファイルパスからテキストを読み取り、解析してFileLocalizerを作成する。
        /// </summary>
        /// <param name="path">ローカライズファイルのパス。</param>
        /// <returns>作成されたFileLocalizerインスタンス。</returns>
        /// <exception cref="ArgumentNullException">ファイルパスがnullの場合にスローされる。</exception>
        public static FileLocalizer FromPath(string path)
        {
            ArgumentNullException.ThrowIfNull(path);
            var text = File.ReadAllText(path);
            var localizations = LocalizationParser.ParseText(text);
            return new(localizations);
        }

        /// <summary>
        /// 指定された複数のファイルパスからテキストを読み取り、解析してFileLocalizerを作成する。
        /// </summary>
        /// <param name="paths">ローカライズファイルのパスのコレクション。</param>
        /// <returns>作成されたFileLocalizerインスタンス。</returns>
        /// <exception cref="ArgumentNullException">ファイルパスのコレクションがnullの場合、またはコレクション内のいずれかのファイルパスがnullの場合にスローされる。</exception>
        public static FileLocalizer FromPaths(IEnumerable<string> paths)
        {
            ArgumentNullException.ThrowIfNull(paths);
            var merged = new Dictionary<string, string>();
            foreach (var path in paths)
            {
                ArgumentNullException.ThrowIfNull(path);
                var text = File.ReadAllText(path);
                var data = LocalizationParser.ParseText(text);
                foreach (var kvp in data)
                {
                    merged[kvp.Key] = kvp.Value;
                }
            }
            return new(merged);
        }

        /// <summary>
        /// 指定されたディレクトリ内のすべてのローカライズファイルを読み取り、解析してFileLocalizerを作成する。
        /// ディレクトリ内のファイルは逆順で読み取られる。
        /// 重複キーは後勝ちとなるため、明示的に上書きしたいファイルがある場合は、FromPathsで順番を指定して読み込む必要がある。
        /// </summary>
        /// <param name="directoryPath">ローカライズファイルが存在するディレクトリのパス。</param>
        /// <returns>作成されたFileLocalizerインスタンス。</returns>
        /// <exception cref="ArgumentNullException">ディレクトリパスがnullの場合にスローされる。</exception>
        public static FileLocalizer FromDirectory(string directoryPath)
        {
            ArgumentNullException.ThrowIfNull(directoryPath);
            var files = Directory
                .EnumerateFiles(directoryPath, "*.yml", SearchOption.AllDirectories)
                .OrderByDescending(f => f);
            return FromPaths(files);
        }


        /// <inheritdoc/>
        public string Localize(string key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _localizations.TryGetValue(key, out var value) ? value : key;
        }

        /// <inheritdoc/>
        public bool TryLocalize(string key, [NotNullWhen(true)] out string value)
        {
            ArgumentNullException.ThrowIfNull(key);
            return _localizations.TryGetValue(key, out value!);
        }
    }
}
```

```Victoria3.Localization\ILocalizer.cs
using System.Diagnostics.CodeAnalysis;

namespace Victoria3.Localization
{
    /// <summary>
    /// キーと対応する文字列のローカライズを提供するインターフェース。
    /// </summary>
    public interface ILocalizer
    {
        /// <summary>
        /// 指定されたキーを対応する文字列に変換する。
        /// 対応する文字列が存在しない場合は、キー自体を返す。
        /// </summary>
        /// <param name="key">変換するキー。</param>
        /// <returns>変換された文字列。見つからない場合はキー自体を返す。</returns>
        /// <exception cref="ArgumentNullException">キーがnullの場合にスローされる。</exception>
        public string Localize(string key);

        /// <summary>
        /// 指定されたキーを対応する文字列に変換し、成功したかどうかを示す。
        /// </summary>
        /// <param name="key">変換するキー。</param>
        /// <param name="value">変換された文字列。見つからない場合はnull。</param>
        /// <returns>変換が成功した場合はtrue、失敗した場合はfalse。</returns>
        /// <exception cref="ArgumentNullException">キーがnullの場合にスローされる。</exception>
        public bool TryLocalize(string key, [NotNullWhen(true)] out string value);
    }
}
```

```Victoria3.Localization\Parsing\LocalizationParser.cs
using System.Diagnostics.CodeAnalysis;

namespace Victoria3.Localization.Parsing
{
    /// <summary>
    /// キーと表示文字列の対応データを解析するクラス。
    /// </summary>
    internal class LocalizationParser
    {
        /// <summary>
        /// 指定されたテキストを解析して、キーと表示文字列の対応データを表す辞書を作成する。
        /// </summary>
        /// <param name="text">解析するテキスト。</param>
        /// <returns>キーと表示文字列の対応データを表す辞書。</returns>
        internal static IReadOnlyDictionary<string, string> ParseText(string text)
        {
            var result = new Dictionary<string, string>();

            foreach (var line in text.AsSpan().EnumerateLines())
            {
                // 行の先頭が '#' で始まる場合はコメント行とみなしてスキップする
                if (line.TrimStart().StartsWith('#')) continue;

                // 正規表現を用いて "l_<somelangname>:" に一致するヘッダー行かどうか調べる
                // ただし、ヘッダー行は TryParseLine が失敗するため、個別の処理は行わない
                // if (Regex.IsMatch(line.Trim(), @"^l_[A-Za-z_]+:$")) continue;

                // 空行はスキップする
                if (line.Trim().IsEmpty) continue;

                // パースに失敗した行は無視する
                if (!TryParseLine(line, out var key, out var value)) continue;

                result[key] = value;
            }
            return result;
        }

        private static bool TryParseLine(ReadOnlySpan<char> line, [NotNullWhen(true)] out string key, [NotNullWhen(true)] out string value)
        {
            // キーの抽出
            // キーは行の先頭から最初のコロンまでの部分で、空文字列であってはならない
            var colonIndex = line.IndexOf(':');
            if (colonIndex == -1)
            {
                key = null!;
                value = null!;
                return false;
            }
            key = line[..colonIndex].Trim().ToString();
            if (string.IsNullOrEmpty(key))
            {
                key = null!;
                value = null!;
                return false;
            }

            // value の始まりを表す最初の引用符の位置を探す
            var afterColon = line[(colonIndex + 1)..];
            var firstQuoteIndex = afterColon.IndexOf('"');
            if (firstQuoteIndex == -1)
            {
                key = null!;
                value = null!;
                return false;
            }
            // version は使用しないため、抽出は行わない
            // var version = afterColon[..firstQuoteIndex].Trim().ToString();

            // value の抽出
            // value は最初の引用符の後から最後の引用符までの部分で、エスケープシーケンスを考慮する
            var afterFirstQuote = afterColon[(firstQuoteIndex + 1)..];
            int lastQuoteIndex = -1;
            // 引用符の終端を探す。引用符の間に \" がある可能性があるため、単純に次の引用符を探すだけでは不十分。
            for (int i = 0; i < afterFirstQuote.Length; i++)
            {
                if (afterFirstQuote[i] == '"')
                {
                    // 前の文字がバックスラッシュでない、またはバックスラッシュが偶数個続いている場合、これは引用符の終端
                    int backslashCount = 0;
                    for (int j = i - 1; j >= 0 && afterFirstQuote[j] == '\\'; j--)
                    {
                        backslashCount++;
                    }
                    if (backslashCount % 2 == 0)
                    {
                        lastQuoteIndex = i;
                        break;
                    }
                }
            }
            if (lastQuoteIndex == -1)
            {
                key = null!;
                value = null!;
                return false;
            }
            value = afterFirstQuote[..lastQuoteIndex].ToString();
            // value 内の \" を " に置換
            value = value.Replace("\\\"", "\"");
            // value 内の \\ を \ に置換
            value = value.Replace("\\\\", "\\");

            // value の後にコメント以外の余分な文字がないか確認する。
            // 余分な文字がある場合はパース失敗とみなす。
            var afterValue = afterFirstQuote[(lastQuoteIndex + 1)..].TrimStart();
            if (!afterValue.IsEmpty && !afterValue.StartsWith('#'))
            {
                key = null!;
                value = null!;
                return false;
            }

            return true;
        }
    }
}
```

