using PdxScriptAnalysis.Diagnostics;
using PdxScriptAnalysis.Lexing;
using PdxScriptAnalysis.Parsing;
using PdxScriptAnalysis.Syntax;
using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis
{
    /// <summary>
    /// Paradox Scriptの解析結果を表すクラス。ソーステキスト、構文木のルートノード、解析中に発生した診断情報を保持する。
    /// </summary>
    public sealed class ScriptTree
    {
        /// <summary>
        /// 解析対象のソーステキスト。ファイルから解析した場合はファイルの内容を、文字列から解析した場合はその文字列を保持する。
        /// </summary>
        public SourceText Source { get; }
        /// <summary>
        /// 構文木のルートノード。
        /// </summary>
        public RootNode Root { get; }
        /// <summary>
        /// 解析中に発生した診断情報のリスト。
        /// </summary>
        public IReadOnlyList<Diagnostic> Diagnostics { get; }


        /// <summary>
        /// 解析中にエラーまたは警告が発生したかどうか。
        /// </summary>
        public bool HasErrorsOrWarnings => Diagnostics.Any(d => d.IsError || d.IsWarning);


        // コンストラクタはprivateで、ファクトリメソッドを通じてのみインスタンス化される。
        private ScriptTree(SourceText source, RootNode root, IReadOnlyList<Diagnostic> diagnostics)
        {
            Source = source;
            Root = root;
            Diagnostics = diagnostics;
        }


        /// <summary>
        /// ファイルから解析を行うファクトリメソッド。指定されたファイルパスからソーステキストを読み込み、解析を行い、ScriptTreeのインスタンスを生成する。
        /// 解析元ファイルのパスを診断情報に追加する。
        /// </summary>
        /// <param name="path">解析対象のファイルパス</param>
        /// <returns>解析結果を表すScriptTreeのインスタンス</returns>
        public static ScriptTree ParseFile(string path)
        {
            var tree = ParseCore(SourceText.FromFile(path));
            var diagnosticsWithPath = tree.Diagnostics
                .Select(d => d with { FilePath = path })
                .ToList();
            return new(tree.Source, tree.Root, diagnosticsWithPath);
        }

        /// <summary>
        /// 文字列から解析を行うファクトリメソッド。指定された文字列をソーステキストとして解析を行い、ScriptTreeのインスタンスを生成する。
        /// </summary>
        /// <param name="text">解析対象の文字列</param>
        /// <returns>解析結果を表すScriptTreeのインスタンス</returns>
        public static ScriptTree ParseText(string text)
            => ParseCore(SourceText.From(text));

        /// <summary>
        /// 既存のソーステキストから解析を行うファクトリメソッド。指定されたソーステキストを解析し、ScriptTreeのインスタンスを生成する。
        /// </summary>
        /// <param name="source">解析対象のソーステキスト</param>
        /// <returns>解析結果を表すScriptTreeのインスタンス</returns>
        public static ScriptTree ParseSource(SourceText source)
            => ParseCore(source);


        // 解析のコアロジック。ソーステキストを受け取り、字句解析、構文解析を行い、構文木と診断情報を生成する。
        private static ScriptTree ParseCore(SourceText source)
        {
            var tokens = new Lexer(source).Tokenize();
            var (root, diagnostics) = new Parser(tokens).Parse();
            return new ScriptTree(source, root, diagnostics);
        }
    }
}
