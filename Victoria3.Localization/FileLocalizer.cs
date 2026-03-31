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
        public string Localize(string? key)
        {
            if (key is null) return string.Empty;
            return _localizations.TryGetValue(key, out var value) ? value : key;
        }

        /// <inheritdoc/>
        public bool TryLocalize(string? key, [NotNullWhen(true)] out string value)
        {
            if (key is null)
            {
                value = null!;
                return false;
            }
            return _localizations.TryGetValue(key, out value!);
        }
    }
}
