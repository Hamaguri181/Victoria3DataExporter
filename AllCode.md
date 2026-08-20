```PdxScriptAnalysis\ScriptTree.cs
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
```

```PdxScriptAnalysis\SyntaxKind.cs
namespace PdxScriptAnalysis
{
    /// <summary>
    /// トークンの種類。
    /// </summary>
    public enum SyntaxKind
    {
        // 構造
        LeftBrace,
        RightBrace,

        // 演算子
        Equals,
        LessThan,
        GreaterThan,
        LessThanEquals,
        GreaterThanEquals,
        NotEquals,
        QuestionEquals,

        // リテラル
        StringLiteral,  // 二重引用符で囲まれた文字列 "..."

        // 識別子・数値
        Atom,

        // その他
        Unknown,
        EndOfFile,
    }
}
```

```PdxScriptAnalysis\SyntaxToken.cs
using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis
{
    /// <summary>
    /// Lexerによって解析されたトークン。
    /// </summary>
    /// <param name="Kind">トークンの種類</param>
    /// <param name="Text">トークンのテキスト</param>
    /// <param name="Span">トークンの位置情報</param>
    /// <param name="LinePosition">トークンの行列位置情報</param>
    public readonly record struct SyntaxToken(SyntaxKind Kind, string Text, TextSpan Span, LinePosition LinePosition)
    {
        /// <summary>
        /// ファイルの終端を表すトークンかどうか。
        /// </summary>
        public bool IsEndOfFile => Kind == SyntaxKind.EndOfFile;

        /// <summary>
        /// 不明なトークンかどうか。
        /// </summary>
        public bool IsUnknown => Kind == SyntaxKind.Unknown;

        /// <summary>
        /// 演算子トークンかどうか。
        /// </summary>
        public bool IsOperator => Kind is SyntaxKind.Equals or SyntaxKind.LessThan or SyntaxKind.GreaterThan or SyntaxKind.LessThanEquals or SyntaxKind.GreaterThanEquals or SyntaxKind.NotEquals or SyntaxKind.QuestionEquals;


        /// <summary>
        /// 整数に変換できるトークンかどうか。整数に変換できる場合は、valueに変換された整数が格納される。
        /// </summary>
        /// <param name="value">変換された整数が格納される変数</param>
        /// <returns>整数に変換できる場合はtrue、それ以外の場合はfalse</returns>
        public bool TryGetInt(out int value)
            => int.TryParse(Text, out value);

        /// <summary>
        /// 十進数に変換できるトークンかどうか。十進数に変換できる場合は、valueに変換された十進数が格納される。
        /// </summary>
        /// <param name="value">変換された十進数が格納される変数</param>
        /// <returns>十進数に変換できる場合はtrue、それ以外の場合はfalse</returns>
        public bool TryGetDecimal(out decimal value)
            => decimal.TryParse(Text, out value);

        /// <summary>
        /// 真偽値に変換できるトークンかどうか。真偽値に変換できる場合は、valueに変換された真偽値が格納される。
        /// </summary>
        /// <param name="value">変換された真偽値が格納される変数</param>
        /// <returns>真偽値に変換できる場合はtrue、それ以外の場合はfalse</returns>
        public bool TryGetBool(out bool value)
        {
            if (Text.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }
            else if (Text.Equals("no", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// 二重引用符で囲まれた文字列リテラルから、引用符を除いた文字列を取得できるかどうか。文字列リテラルであれば、valueに引用符を除いた文字列が格納される。
        /// </summary>
        /// <param name="value">取得された文字列が格納される変数</param>
        /// <returns>文字列リテラルから文字列を取得できる場合はtrue、それ以外の場合はfalse</returns>
        public bool TryGetString(out string value)
        {
            if (Kind == SyntaxKind.StringLiteral && Text.Length >= 2 && Text[0] == '"' && Text[^1] == '"')
            {
                value = Text[1..^1];
                return true;
            }
            else
            {
                value = default!;
                return false;
            }
        }

        public override string ToString() => $"{Kind} \"{Text}\" {Span}";
    }
}
```

```PdxScriptAnalysis\Diagnostics\Diagnostic.cs
using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Diagnostics
{
    /// <summary>
    /// 構文解析や意味解析の過程で発生する診断情報を表すクラス。
    /// </summary>
    /// <param name="Severity">診断の重大度を表す値。</param>
    /// <param name="Message">診断メッセージ。</param>
    /// <param name="Span">診断が発生したソースコードの範囲。</param>
    /// <param name="LinePosition">診断が発生した行位置情報。</param>
    /// <param name="FilePath">解析データの元のファイルパス。ファイル以外からの解析の場合はnull。</param>
    public sealed record Diagnostic(
        DiagnosticSeverity Severity,
        string Message,
        TextSpan Span,
        LinePosition LinePosition,
        string? FilePath = null)
    {
        /// <summary>
        /// 診断の重大度が情報であるかどうか。
        /// </summary>
        public bool IsInfo => Severity == DiagnosticSeverity.Info;

        /// <summary>
        /// 診断の重大度がエラーであるかどうか。
        /// </summary>
        public bool IsError => Severity == DiagnosticSeverity.Error;

        /// <summary>
        /// 診断の重大度が警告であるかどうか。
        /// </summary>
        public bool IsWarning => Severity == DiagnosticSeverity.Warning;

        public override string ToString()
                => $"{Severity}: {Message} at {LinePosition}{(FilePath is not null ? $" in {FilePath}" : "")}";
    }
}
```

```PdxScriptAnalysis\Diagnostics\DiagnosticSeverity.cs
namespace PdxScriptAnalysis.Diagnostics
{
    /// <summary>
    /// 診断結果の重大度を表す列挙型。
    /// </summary>
    public enum DiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }
}
```

```PdxScriptAnalysis\Lexing\Lexer.cs
using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Lexing
{
    /// <summary>
    /// 字句解析器。ソーステキストをトークンのリストに変換する。
    /// </summary>
    /// <param name="source">解析対象のソーステキスト</param>
    internal class Lexer(SourceText source)
    {
        private readonly SourceText _source = source;
        private int _position = 0;

        private char Current => _source[_position];

        /// <summary>
        /// トークンのリストを取得する。
        /// </summary>
        /// <returns>トークンのリスト</returns>
        internal IReadOnlyList<SyntaxToken> Tokenize()
        {
            var tokens = new List<SyntaxToken>();
            while (true)
            {
                var token = NextToken();
                tokens.Add(token);
                if (token.IsEndOfFile) break;
            }
            return tokens;
        }

        private SyntaxToken NextToken()
        {
            SkipTrivia();

            if (_position >= _source.Length) return CreateToken(SyntaxKind.EndOfFile, _position, 0);

            return Current switch
            {
                '{' => CreateToken(SyntaxKind.LeftBrace, _position++, 1),
                '}' => CreateToken(SyntaxKind.RightBrace, _position++, 1),
                '=' => CreateToken(SyntaxKind.Equals, _position++, 1),
                '<' => ReadLessThan(),
                '>' => ReadGreaterThan(),
                '!' => ReadNotEquals(),
                '?' => ReadQuestionEquals(),
                '"' => ReadStringLiteral(),
                _ => ReadAtom(),
            };
        }

        private SyntaxToken ReadLessThan()
        {
            var start = _position;
            Advance();
            if (_position < _source.Length && Current == '=')
            {
                Advance();
                return CreateToken(SyntaxKind.LessThanEquals, start, 2);
            }
            return CreateToken(SyntaxKind.LessThan, start, 1);
        }

        private SyntaxToken ReadGreaterThan()
        {
            var start = _position;
            Advance();
            if (_position < _source.Length && Current == '=')
            {
                Advance();
                return CreateToken(SyntaxKind.GreaterThanEquals, start, 2);
            }
            return CreateToken(SyntaxKind.GreaterThan, start, 1);
        }

        private SyntaxToken ReadNotEquals()
        {
            var start = _position;
            Advance();
            if (_position < _source.Length && Current == '=')
            {
                Advance();
                return CreateToken(SyntaxKind.NotEquals, start, 2);
            }
            return CreateToken(SyntaxKind.Unknown, start, 1);
        }

        private SyntaxToken ReadQuestionEquals()
        {
            var start = _position;
            Advance();
            if (_position < _source.Length && Current == '=')
            {
                Advance();
                return CreateToken(SyntaxKind.QuestionEquals, start, 2);
            }
            return CreateToken(SyntaxKind.Unknown, start, 1);
        }

        private SyntaxToken ReadStringLiteral()
        {
            var start = _position;
            Advance(); // 開始の二重引用符をスキップ

            while (_position < _source.Length && Current != '"')
            {
                Advance();
            }

            // 終了の二重引用符が見つからない場合は、文字列リテラルの終わりまでをトークン化する
            if (_position >= _source.Length) return CreateToken(SyntaxKind.Unknown, start, _position - start);

            if (_position < _source.Length)
            {
                Advance(); // 終了の二重引用符をスキップ
            }
            return CreateToken(SyntaxKind.StringLiteral, start, _position - start);
        }

        private SyntaxToken ReadAtom()
        {
            var start = _position;
            while (_position < _source.Length && IsAtomChar(Current))
            {
                Advance();
            }

            if (start == _position) return CreateToken(SyntaxKind.Unknown, _position++, 1);

            return CreateToken(SyntaxKind.Atom, start, _position - start);
        }

        private static bool IsAtomChar(char c)
            => !(char.IsWhiteSpace(c) || c is '#' or '"' or '{' or '}' or '=' or '<' or '>' or '!' or '?');

        private void Advance() => _position++;

        private void SkipTrivia()
        {
            while (_position < _source.Length)
            {
                if (char.IsWhiteSpace(Current))
                {
                    Advance();
                }
                else if (Current == '#')
                {
                    while (_position < _source.Length && Current != '\n')
                    {
                        Advance();
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private SyntaxToken CreateToken(SyntaxKind kind, int start, int length)
        {
            var span = new TextSpan(start, length);
            var text = _source.GetSubText(span);
            var linePosition = _source.GetLinePosition(span.Start);
            return new SyntaxToken(kind, text, span, linePosition);
        }
    }
}
```

```PdxScriptAnalysis\Parsing\Parser.cs
using PdxScriptAnalysis.Diagnostics;
using PdxScriptAnalysis.Syntax;
using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Parsing
{
    /// <summary>
    /// 構文解析器。トークンのリストを構文木に変換する。
    /// </summary>
    /// <param name="tokens">解析対象のトークンのリスト。</param>
    internal class Parser(IReadOnlyList<SyntaxToken> tokens)
    {
        private readonly IReadOnlyList<SyntaxToken> _tokens = tokens;
        private readonly List<Diagnostic> _diagnostics = [];
        private int _position = 0;

        private SyntaxToken Current => _tokens[_position];

        /// <summary>
        /// トークンのリストを構文木に変換する。解析中に発生した診断情報も返す。
        /// </summary>
        /// <returns>構文木のルートノードと診断情報のリストを含むタプル。</returns>
        internal (RootNode Root, IReadOnlyList<Diagnostic> Diagnostics) Parse()
        {
            _diagnostics.Clear();
            var topNodes = new List<SyntaxNode>();

            // 終端トークンにあたるまで解析する
            while (Current.Kind != SyntaxKind.EndOfFile)
            {
                var node = ParseNode();
                if (node is not null)
                {
                    topNodes.Add(node);
                }
            }

            var span = topNodes.Count == 0
                ? new TextSpan(0, 0)
                : TextSpan.Union(topNodes[0].Span, topNodes[^1].Span);

            return (new RootNode(topNodes, span), _diagnostics);
        }

        private SyntaxNode? ParseNode()
        {
            return Current.Kind switch
            {
                SyntaxKind.LeftBrace => ParseBlock(),
                SyntaxKind.Atom or SyntaxKind.StringLiteral => ParseScalarOrProperty(),
                _ => ParseUnexpected()
            };
        }

        private SyntaxNode ParseScalarOrProperty()
        {
            return Peek().IsOperator
                ? ParseProperty()
                : ParseScalar();
        }

        private BlockNode ParseBlock()
        {
            var childNodes = new List<SyntaxNode>();

            var leftBrace = Current;
            Advance(); // { を読み飛ばす

            while (Current.Kind != SyntaxKind.RightBrace)
            {
                if (Current.Kind == SyntaxKind.EndOfFile)
                {
                    AddError("Unexpected end of file. Expected '}' to close the block.", Current.Span, Current.LinePosition);
                    var span = TextSpan.Union(leftBrace.Span, Current.Span);
                    return new BlockNode(childNodes, span);
                }
                // ブロックの中身を解析する
                var node = ParseNode();
                if (node is null) break;
                childNodes.Add(node);
            }

            var rightBrace = Current;
            Advance(); // } を読み飛ばす

            var blockSpan = TextSpan.Union(leftBrace.Span, rightBrace.Span);
            return new BlockNode(childNodes, blockSpan);
        }

        private ScalarNode ParseScalar()
        {
            var token = Current;
            Advance(); // トークンを読み飛ばす
            return new ScalarNode(token, token.Span);
        }

        private PropertyNode ParseProperty()
        {
            var key = Current;
            Advance(); // キーを読み飛ばす

            var op = Current;
            Advance(); // 演算子を読み飛ばす

            if (Current.Kind == SyntaxKind.LeftBrace)
            {
                var block = ParseBlock();
                var span = TextSpan.Union(key.Span, block.Span);
                return new BlockPropertyNode(key, op, block, span);
            }
            else if (Current.Kind == SyntaxKind.Atom && Peek().Kind == SyntaxKind.LeftBrace)
            {
                var qualifier = Current;
                Advance(); // 修飾子を読み飛ばす
                var block = ParseBlock();
                var span = TextSpan.Union(key.Span, block.Span);
                return new TypedBlockPropertyNode(key, op, qualifier, block, span);
            }
            else if (Current.Kind == SyntaxKind.Atom || Current.Kind == SyntaxKind.StringLiteral)
            {
                var scalar = ParseScalar();
                var span = TextSpan.Union(key.Span, scalar.Span);
                return new ScalarPropertyNode(key, op, scalar, span);
            }
            else
            {
                AddError($"Invalid property value: \"{Current.Text}\"", Current.Span, Current.LinePosition);
                var errorToken = CreateMissing(Current.Span, Current.LinePosition);
                var errorScalar = new ScalarNode(errorToken, errorToken.Span);
                var span = TextSpan.Union(key.Span, errorScalar.Span);
                return new ScalarPropertyNode(key, op, errorScalar, span);
            }
        }

        // 予期しないトークンが出現した場合のエラーハンドリング
        private SyntaxNode? ParseUnexpected()
        {
            AddError($"Unexpected token: \"{Current.Text}\"", Current.Span, Current.LinePosition);
            Advance();
            return null;
        }


        private void Advance(int count = 1)
        {
            _position = Math.Min(_position + count, _tokens.Count - 1);
        }

        private SyntaxToken Peek(int offset = 1)
        {
            var index = Math.Min(_position + offset, _tokens.Count - 1);
            return _tokens[index];
        }

        // エラー回復のためのダミートークンを作成する
        private static SyntaxToken CreateMissing(TextSpan span, LinePosition linePosition)
            => new(SyntaxKind.Unknown, string.Empty, new TextSpan(span.Start, 0), linePosition);

        // エラー診断を追加するためのヘルパーメソッド
        private void AddError(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, message, span, linePosition));
    }
}
```

```PdxScriptAnalysis\Syntax\BlockNode.cs
using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Syntax
{
    /// <summary>
    /// { ... } で囲まれた複数のノードを表す構文ノード。子ノードは、ブロック内のノードを順番に列挙する。
    /// </summary>
    /// <param name="Children">ブロック内の子ノードのリスト</param>
    /// <param name="Span">ソーステキスト上の範囲を表すスパン</param>
    public sealed record BlockNode(
        IReadOnlyList<SyntaxNode> Children,
        TextSpan Span)
        : SyntaxNode(Span)
    {
        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
            => visitor.VisitBlock(this);
        public override void Accept(SyntaxWalker walker)
            => walker.VisitBlock(this);
        public override IEnumerable<SyntaxNode> ChildNodes()
            => Children;
        public override LinePosition LinePosition
            => Children.Count > 0 ? Children[0].LinePosition : new LinePosition(0, Span.Length);

        public override string ToString()
            => $"{GetType().Name}: {Children.Count} children at {Span}";
    }
}
```

```PdxScriptAnalysis\Syntax\BlockPropertyNode.cs
using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Syntax
{
    /// <summary>
    /// 右辺がブロックであるプロパティノード。
    /// </summary>
    /// <param name="Key">プロパティの名前を表すトークン</param>
    /// <param name="Operator">プロパティの演算子を表すトークン</param>
    /// <param name="Value">プロパティの値を表すブロックノード</param>
    /// <param name="Span">ソーステキスト上の範囲を表すスパン</param>
    public sealed record BlockPropertyNode(
        SyntaxToken Key,
        SyntaxToken Operator,
        BlockNode Value,
        TextSpan Span)
        : PropertyNode(Key, Operator, Span)
    {
        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
            => visitor.VisitBlockProperty(this);
        public override void Accept(SyntaxWalker walker)
            => walker.VisitBlockProperty(this);
        public override IEnumerable<SyntaxNode> ChildNodes()
            => [Value];

        public override string ToString()
            => $"{GetType().Name}: {Key.Text} {Operator.Text} {{ {Value.Children.Count} children }} at {Span}";
    }
}
```

```PdxScriptAnalysis\Syntax\PropertyNode.cs
using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Syntax
{
    /// <summary>
    /// Key Operator Valueの形式を持つプロパティノード。
    /// Keyはプロパティの名前を表すトークン、Operatorはプロパティの演算子を表すトークンである。
    /// </summary>
    /// <param name="Key">プロパティの名前を表すトークン</param>
    /// <param name="Operator">プロパティの演算子を表すトークン</param>
    /// <param name="Span">ソーステキスト上の範囲を表すスパン</param>
    public abstract record PropertyNode(
        SyntaxToken Key,
        SyntaxToken Operator,
        TextSpan Span)
        : SyntaxNode(Span)
    {
        public override LinePosition LinePosition
            => Key.LinePosition;

        public override string ToString()
            => $"{GetType().Name}: {Key.Text} {Operator.Text} ... at {Span}";
    }
}
```

```PdxScriptAnalysis\Syntax\RootNode.cs
using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Syntax
{
    /// <summary>
    /// ファイル全体を表す構文ノード。子ノードは、ファイル内のトップレベルノードを順番に列挙する。
    /// </summary>
    /// <param name="Children">ファイル内のトップレベルノードのリスト</param>
    /// <param name="Span">ソーステキスト上の範囲を表すスパン</param>
    public sealed record RootNode(
        IReadOnlyList<SyntaxNode> Children,
        TextSpan Span)
        : SyntaxNode(Span)
    {
        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
            => visitor.VisitRoot(this);
        public override void Accept(SyntaxWalker walker)
            => walker.VisitRoot(this);
        public override IEnumerable<SyntaxNode> ChildNodes()
            => Children;
        public override LinePosition LinePosition
            => Children.Count > 0 ? Children[0].LinePosition : new LinePosition(0, Span.Length);
    }
}
```

```PdxScriptAnalysis\Syntax\ScalarNode.cs
using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Syntax
{
    /// <summary>
    /// トークン1つを表す構文ノード。
    /// </summary>
    /// <param name="Token">スカラー値を表すトークン</param>
    /// <param name="Span">ソーステキスト上の範囲を表すスパン</param>
    public sealed record ScalarNode(
        SyntaxToken Token,
        TextSpan Span)
        : SyntaxNode(Span)
    {
        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
            => visitor.VisitScalar(this);
        public override void Accept(SyntaxWalker walker)
            => walker.VisitScalar(this);
        public override IEnumerable<SyntaxNode> ChildNodes()
            => [];
        public override LinePosition LinePosition
            => Token.LinePosition;

        public override string ToString()
            => $"{GetType().Name}: {Token.Text} at {Span}";
    }
}
```

```PdxScriptAnalysis\Syntax\ScalarPropertyNode.cs
using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Syntax
{
    /// <summary>
    /// 右辺がスカラー値であるプロパティノード。
    /// </summary>
    /// <param name="Key">プロパティの名前を表すトークン</param>
    /// <param name="Operator">プロパティの演算子を表すトークン</param>
    /// <param name="Value">プロパティの値を表すスカラー値ノード</param>
    /// <param name="Span">ソーステキスト上の範囲を表すスパン</param>
    public sealed record ScalarPropertyNode(
        SyntaxToken Key,
        SyntaxToken Operator,
        ScalarNode Value,
        TextSpan Span)
        : PropertyNode(Key, Operator, Span)
    {
        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
            => visitor.VisitScalarProperty(this);
        public override void Accept(SyntaxWalker walker)
            => walker.VisitScalarProperty(this);
        public override IEnumerable<SyntaxNode> ChildNodes()
            => [Value];

        public override string ToString()
            => $"{GetType().Name}: {Key.Text} {Operator.Text} {Value.Token.Text} at {Span}";
    }
}
```

```PdxScriptAnalysis\Syntax\SyntaxNode.cs
using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Syntax
{
    /// <summary>
    /// 全ての構文ノードの基底クラス。構文ノードは、ソーステキスト上の位置範囲を表す<see cref="TextSpan"/>を持ち、構文ツリー内の子ノードを列挙できる。
    /// </summary>
    /// <param name="Span">ソーステキスト上の位置範囲を表す<see cref="TextSpan"/>。</param>
    public abstract record SyntaxNode(TextSpan Span)
    {
        /// <summary>
        /// Visitorのエントリーポイント。構文ノードの種類に応じて、適切なVisitメソッドが呼び出される。
        /// </summary>
        /// <typeparam name="TResult">Visitorが返す結果の型。</typeparam>
        /// <param name="visitor">訪問するVisitor。</param>
        /// <returns>Visitorが返す結果。Visitorが結果を返さない場合は<see langword="default"/>。</returns>
        public abstract TResult Accept<TResult>(SyntaxVisitor<TResult> visitor);

        /// <summary>
        /// Walkerのエントリーポイント。構文ノードの種類に応じて、適切なVisitメソッドが呼び出される。
        /// </summary>
        /// <param name="walker">訪問するWalker。</param>
        public abstract void Accept(SyntaxWalker walker);

        /// <summary>
        /// この構文ノードの子ノードを列挙する。子ノードは、構文ツリー内でこのノードの下に位置するノードであり、直接的な親子関係にあるものを指す。
        /// </summary>
        /// <returns>この構文ノードの子ノードの列挙。</returns>
        public abstract IEnumerable<SyntaxNode> ChildNodes();

        /// <summary>
        /// この構文ノードの最初のトークンの行列位置を返す。
        /// </summary>
        public abstract LinePosition LinePosition { get; }

        public override string ToString()
            => $"{GetType().Name} at {Span}";
    }
}
```

```PdxScriptAnalysis\Syntax\SyntaxVisitor.cs
namespace PdxScriptAnalysis.Syntax
{
    /// <summary>
    /// 構文ノードを訪問するためのビジターパターンの抽象クラス。
    /// 構文ノードの種類ごとに Visit メソッドが用意されており、必要に応じてオーバーライドして使用する。
    /// </summary>
    /// <typeparam name="TResult">訪問の結果の型</typeparam>
    public abstract class SyntaxVisitor<TResult>
    {
        /// <summary>
        /// 指定された構文ノードを訪問し、その結果を返す。
        /// </summary>
        /// <param name="node">訪問する構文ノード</param>
        /// <returns>訪問の結果</returns>
        public virtual TResult Visit(SyntaxNode node)
        {
            if (node is null) return default!;
            return node.Accept(this);
        }

        /// <summary>
        /// デフォルトの訪問処理を提供するメソッド。特定のノードタイプに対してオーバーライドされない場合に呼び出される。
        /// 結果の型によっては、nullを返すこともある。
        /// </summary>
        /// <param name="node">訪問する構文ノード</param>
        /// <returns>訪問の結果</returns>
        protected virtual TResult DefaultVisit(SyntaxNode node)
            => default!;

        /// <summary>
        /// スカラーノードを訪問するためのメソッド。
        /// </summary>
        /// <param name="node">訪問するスカラーノード</param>
        /// <returns>訪問の結果</returns>
        protected internal virtual TResult VisitScalar(ScalarNode node)
            => DefaultVisit(node);

        /// <summary>
        /// ブロックノードを訪問するためのメソッド。
        /// </summary>
        /// <param name="node">訪問するブロックノード</param>
        /// <returns>訪問の結果</returns>
        protected internal virtual TResult VisitBlock(BlockNode node)
            => DefaultVisit(node);

        /// <summary>
        /// プロパティノードを訪問するためのメソッド。
        /// </summary>
        /// <param name="node">訪問するプロパティノード</param>
        /// <returns>訪問の結果</returns>
        protected internal virtual TResult VisitProperty(PropertyNode node)
            => DefaultVisit(node);

        /// <summary>
        /// スカラープロパティノードを訪問するためのメソッド。デフォルトでは、プロパティノードの訪問処理を呼び出す。
        /// </summary>
        /// <param name="node">訪問するスカラープロパティノード</param>
        /// <returns>訪問の結果</returns>
        protected internal virtual TResult VisitScalarProperty(ScalarPropertyNode node)
            => VisitProperty(node);

        /// <summary>
        /// ブロックプロパティノードを訪問するためのメソッド。デフォルトでは、プロパティノードの訪問処理を呼び出す。
        /// </summary>
        /// <param name="node">訪問するブロックプロパティノード</param>
        /// <returns>訪問の結果</returns>
        protected internal virtual TResult VisitBlockProperty(BlockPropertyNode node)
            => VisitProperty(node);
        
        /// <summary>
        /// 型付きブロックプロパティノードを訪問するためのメソッド。デフォルトでは、プロパティノードの訪問処理を呼び出す。
        /// </summary>
        /// <param name="node">訪問する型付きブロックプロパティノード</param>
        /// <returns>訪問の結果</returns>
        protected internal virtual TResult VisitTypedBlockProperty(TypedBlockPropertyNode node)
            => VisitProperty(node);
        
        /// <summary>
        /// ルートノードを訪問するためのメソッド。
        /// </summary>
        /// <param name="node">訪問するルートノード</param>
        /// <returns>訪問の結果</returns>
        protected internal virtual TResult VisitRoot(RootNode node)
            => DefaultVisit(node);
    }
}
```

```PdxScriptAnalysis\Syntax\SyntaxWalker.cs
namespace PdxScriptAnalysis.Syntax
{
    /// <summary>
    /// 構文ノードを巡回するためのクラス。
    /// 構文ノードの種類ごとに Visit メソッドが用意されており、必要に応じてオーバーライドして使用する。
    /// </summary>
    public abstract class SyntaxWalker
    {
        /// <summary>
        /// 指定された構文ノードを訪問する。ノードが null の場合は何もしない。
        /// </summary>
        /// <param name="node">訪問する構文ノード</param>
        public virtual void Visit(SyntaxNode node)
        {
            if (node is null) return;
            node.Accept(this);
        }

        /// <summary>
        /// デフォルトの訪問処理。ノードの種類に関係なく、子ノードをすべて訪問する。
        /// </summary>
        /// <param name="node">訪問する構文ノード</param>
        protected virtual void DefaultVisit(SyntaxNode node)
        {
            foreach (var child in node.ChildNodes())
            {
                Visit(child);
            }
        }

        /// <summary>
        /// スカラーノードの訪問処理。デフォルトでは DefaultVisit を呼び出す。
        /// </summary>
        /// <param name="node">訪問するスカラーノード</param>
        protected internal virtual void VisitScalar(ScalarNode node)
            => DefaultVisit(node);

        /// <summary>
        /// ブロックノードの訪問処理。デフォルトでは DefaultVisit を呼び出す。
        /// </summary>
        /// <param name="node">訪問するブロックノード</param>
        protected internal virtual void VisitBlock(BlockNode node)
            => DefaultVisit(node);

        /// <summary>
        /// プロパティノードの訪問処理。デフォルトでは DefaultVisit を呼び出す。
        /// </summary>
        /// <param name="node">訪問するプロパティノード</param>
        protected internal virtual void VisitProperty(PropertyNode node)
            => DefaultVisit(node);

        /// <summary>
        /// スカラープロパティノードの訪問処理。デフォルトではプロパティノードの訪問処理を呼び出す。
        /// </summary>
        /// <param name="node">訪問するスカラープロパティノード</param>
        protected internal virtual void VisitScalarProperty(ScalarPropertyNode node)
            => VisitProperty(node);

        /// <summary>
        /// ブロックプロパティノードの訪問処理。デフォルトではプロパティノードの訪問処理を呼び出す。
        /// </summary>
        /// <param name="node">訪問するブロックプロパティノード</param>
        protected internal virtual void VisitBlockProperty(BlockPropertyNode node)
            => VisitProperty(node);

        /// <summary>
        /// 型付きブロックプロパティノードの訪問処理。デフォルトではプロパティノードの訪問処理を呼び出す。
        /// </summary>
        /// <param name="node">訪問する型付きブロックプロパティノード</param>
        protected internal virtual void VisitTypedBlockProperty(TypedBlockPropertyNode node)
            => VisitProperty(node);

        /// <summary>
        /// ルートノードの訪問処理。デフォルトでは DefaultVisit を呼び出す。
        /// </summary>
        /// <param name="node">訪問するルートノード</param>
        protected internal virtual void VisitRoot(RootNode node)
            => DefaultVisit(node);
    }
}
```

```PdxScriptAnalysis\Syntax\TypedBlockPropertyNode.cs
using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Syntax
{
    /// <summary>
    /// 右辺が修飾子付きブロックであるプロパティノード。
    /// 例えば、color = hsv {10 20 30 } のようなプロパティを表す。
    /// </summary>
    /// <param name="Key">プロパティの名前を表すトークン</param>
    /// <param name="Operator">プロパティの演算子を表すトークン</param>
    /// <param name="TypeQualifier">プロパティの型修飾子を表すトークン</param>
    /// <param name="Value">プロパティの値を表すブロックノード</param>
    /// <param name="Span">ソーステキスト上の範囲を表すスパン</param>
    public sealed record TypedBlockPropertyNode(
        SyntaxToken Key,
        SyntaxToken Operator,
        SyntaxToken TypeQualifier,
        BlockNode Value,
        TextSpan Span)
        : PropertyNode(Key, Operator, Span)
    {
        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor)
            => visitor.VisitTypedBlockProperty(this);
        public override void Accept(SyntaxWalker walker)
            => walker.VisitTypedBlockProperty(this);
        public override IEnumerable<SyntaxNode> ChildNodes()
            => [Value];

        public override string ToString()
            => $"{GetType().Name}: {Key.Text} {Operator.Text} {TypeQualifier.Text} {{ {Value.Children.Count} children }} at {Span}";
    }
}
```

```PdxScriptAnalysis\Text\LinePosition.cs
namespace PdxScriptAnalysis.Text
{
    /// <summary>
    /// ソーステキスト上の行・列を表す構造体。
    /// 文字列として表示される際には1始まりで提供される。
    /// </summary>
    public readonly record struct LinePosition : IComparable<LinePosition>
    {
        /// <summary>
        /// <see cref="LinePosition"/>の新しいインスタンスを初期化する。lineとcharacterは0以上でなければならない。
        /// </summary>
        /// <param name="line">行番号。0以上でなければならない。</param>
        /// <param name="character">列番号。0以上でなければならない。</param>
        /// <exception cref="ArgumentOutOfRangeException">lineまたはcharacterが0未満の場合にスローされる。</exception>
        public LinePosition(int line, int character)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(line);
            ArgumentOutOfRangeException.ThrowIfNegative(character);
            Line = line;
            Character = character;
        }


        /// <summary>
        /// 行番号。ソーステキストの先頭は0で、行が1つ増えるごとに1ずつ増える。行の終端はソーステキストの行数と同じ値になる。
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// 列番号。行の先頭は0で、行内の文字が1ずつ増える。行の終端は行の長さと同じ値になる。
        /// </summary>
        public int Character { get; }


        public override string ToString() => $"{Line + 1}:{Character + 1}";

        public int CompareTo(LinePosition other)
        {
            int lineComparison = Line.CompareTo(other.Line);
            return (lineComparison != 0) ? lineComparison : Character.CompareTo(other.Character);
        }


        public static bool operator <(LinePosition left, LinePosition right) => left.CompareTo(right) < 0;
        public static bool operator >(LinePosition left, LinePosition right) => left.CompareTo(right) > 0;
        public static bool operator <=(LinePosition left, LinePosition right) => left.CompareTo(right) <= 0;
        public static bool operator >=(LinePosition left, LinePosition right) => left.CompareTo(right) >= 0;
    }
}
```

```PdxScriptAnalysis\Text\SourceText.cs
namespace PdxScriptAnalysis.Text
{
    /// <summary>
    /// パース対象のソーステキストを表す。
    /// 文字列ラップ・行列変換キャッシュ・部分文字列取得を提供する。
    /// ファイルから生成された場合はファイルパス情報も持つ。
    /// </summary>
    public sealed class SourceText
    {
        /// <summary>
        /// パース対象のソーステキスト。
        /// </summary>
        public string Text { get; }
        /// <summary>
        /// ソーステキストが生成された元のファイルパス。ファイル以外から生成された場合はnull。
        /// </summary>
        public string? FilePath { get; private init; } = null;


        // コンストラクタはprivateで、ファクトリメソッドを通じてのみインスタンス化される。
        private SourceText(string text)
        {
            Text = text;
        }


        /// <summary>
        /// 文字列から<see cref="SourceText"/>を作成する。nullはArgumentNullExceptionになる。
        /// </summary>
        /// <param name="text">作成するソーステキストの文字列。</param>
        /// <returns>作成された<see cref="SourceText"/>インスタンス。</returns>
        /// <exception cref="ArgumentNullException">textがnullの場合にスローされる。</exception>
        public static SourceText From(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            return new SourceText(text);
        }

        /// <summary>
        /// ファイルから<see cref="SourceText"/>を作成する。pathがnullはArgumentNullExceptionになる。ファイルが存在しない場合はFileNotFoundExceptionになる。
        /// </summary>
        /// <param name="path">作成するソーステキストのファイルパス。</param>
        /// <returns>作成された<see cref="SourceText"/>インスタンス。</returns>
        /// <exception cref="ArgumentNullException">pathがnullの場合にスローされる。</exception>
        /// <exception cref="FileNotFoundException">ファイルが存在しない場合にスローされる。</exception>
        public static SourceText FromFile(string path)
        {
            ArgumentNullException.ThrowIfNull(path);
            var content = File.ReadAllText(path);
            return new SourceText(content) { FilePath = path };
        }

        /// <summary>
        /// ソーステキストの長さ。
        /// </summary>
        public int Length => Text.Length;

        /// <summary>
        /// ソーステキストの指定した位置の文字。インデックスが範囲外の場合はIndexOutOfRangeExceptionになる。
        /// </summary>
        /// <param name="index">取得する文字のインデックス。</param>
        /// <returns>指定したインデックスの文字。</returns>
        public char this[int index] => Text[index];


        /// <summary>
        /// テキストスパンに対応する部分文字列を返す。スパンが範囲外の場合はArgumentOutOfRangeExceptionになる。
        /// </summary>
        /// <param name="span">取得する部分文字列の範囲を表す<see cref="TextSpan"/>。</param>
        /// <returns>指定した範囲の部分文字列。</returns>
        /// <exception cref="ArgumentOutOfRangeException">spanが範囲外の場合にスローされる。</exception>
        public string GetSubText(TextSpan span)
        {
            if (span.End > Length) throw new ArgumentOutOfRangeException(nameof(span), "TextSpan is out of range.");
            var spanLength = span.Length;
            return span.IsEmpty ? string.Empty :
                spanLength == Length ? Text :
                Text.Substring(span.Start, spanLength);
        }

        /// <summary>
        /// テキスト内の絶対位置を行・列位置に変換する。位置が範囲外の場合はArgumentOutOfRangeExceptionになる。
        /// </summary>
        /// <param name="position">変換する絶対位置。</param>
        /// <returns>指定した位置に対応する行・列位置。</returns>
        /// <exception cref="ArgumentOutOfRangeException">positionが範囲外の場合にスローされる。</exception>
        public LinePosition GetLinePosition(int position)
        {
            if (position > Length) throw new ArgumentOutOfRangeException(nameof(position), "Position is out of range.");
            int line = 0, character = 0;
            for (int i = 0; i < position; i++)
            {
                if (Text[i] == '\r')
                {
                    continue;
                }
                else if (Text[i] == '\n')
                {
                    line++;
                    character = 0;
                }
                else
                {
                    character++;
                }
            }
            return new LinePosition(line, character);
        }

        /// <summary>
        /// 行・列位置をテキスト内の絶対位置に変換する。行・列位置が範囲外の場合はArgumentOutOfRangeExceptionになる。
        /// </summary>
        /// <param name="linePosition">変換する行・列位置。</param>
        /// <returns>指定した行・列位置に対応する絶対位置。</returns>
        /// <exception cref="ArgumentOutOfRangeException">linePositionが範囲外の場合にスローされる。</exception>
        public int GetPosition(LinePosition linePosition)
        {
            int line = 0, character = 0;
            for (int i = 0; i < Length; i++)
            {
                if (line > linePosition.Line || (line == linePosition.Line && character > linePosition.Character))
                {
                    throw new ArgumentOutOfRangeException(nameof(linePosition), "LinePosition is out of range.");
                }
                if (line == linePosition.Line && character == linePosition.Character)
                {
                    return i;
                }

                if (Text[i] == '\r')
                {
                    continue;
                }
                else if (Text[i] == '\n')
                {
                    line++;
                    character = 0;
                }
                else
                {
                    character++;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(linePosition), "LinePosition is out of range.");
        }

        public override string ToString() => Text;
    }
}
```

```PdxScriptAnalysis\Text\TextSpan.cs
namespace PdxScriptAnalysis.Text
{
    /// <summary>
    /// ソーステキスト上の位置範囲を表す。
    /// 開始位置と文字数で定義される。開始位置はソーステキストの先頭からの文字数で、0から始まる。長さは範囲内の文字数で、0以上でなければならない。
    /// </summary>
    public readonly record struct TextSpan
    {
        /// <summary>
        /// <see cref="TextSpan"/>の新しいインスタンスを初期化する。startは0以上でなければならない。lengthは0以上でなければならない。
        /// </summary>
        /// <param name="start">開始位置。</param>
        /// <param name="length">範囲の長さ。</param>
        /// <exception cref="ArgumentOutOfRangeException">startまたはlengthが負の値の場合にスローされる。</exception>
        public TextSpan(int start, int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(start);
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            Start = start;
            Length = length;
        }

        /// <summary>
        /// 開始位置。ソーステキストの先頭からの文字数で、0から始まる。
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// 範囲の長さ。範囲内の文字数で、0以上でなければならない。
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// 終了位置。ソーステキストの先頭からの文字数で、0から始まる。終了位置は範囲内の最後の文字の次の位置になる。
        /// </summary>
        public int End => Start + Length;

        /// <summary>
        /// このテキストスパンが空であるかどうか。空のテキストスパンは、開始位置と終了位置が同じで、範囲内に文字がないことを意味する。
        /// </summary>
        public bool IsEmpty => Length == 0;


        /// <summary>
        /// 指定した開始位置と終了位置からテキストスパンを作成する。
        /// 開始位置と終了位置はソーステキストの先頭からの文字数で、0から始まる。終了位置は開始位置以上でなければならない。
        /// </summary>
        /// <param name="start">開始位置。</param>
        /// <param name="end">終了位置。</param>
        /// <returns>指定した範囲を表すテキストスパン。</returns>
        /// <exception cref="ArgumentException">終了位置が開始位置より小さい場合にスローされる。</exception>
        /// <exception cref="ArgumentOutOfRangeException">startまたはendが負の値の場合にスローされる。</exception>
        public static TextSpan FromBounds(int start, int end)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(start);
            ArgumentOutOfRangeException.ThrowIfNegative(end);
            if (end < start) throw new ArgumentException("end must be greater than or equal to start.");
            return new TextSpan(start, end - start);
        }


        /// <summary>
        /// 2つのテキストスパンを結合して、両方のテキストスパンを完全に含む最小のテキストスパンを作成する。
        /// </summary>
        /// <param name="span1">1つ目のテキストスパン。</param>
        /// <param name="span2">2つ目のテキストスパン。</param>
        /// <returns>2つのテキストスパンを完全に含む最小のテキストスパン。</returns>
        public static TextSpan Union(TextSpan span1, TextSpan span2)
        {
            int start = Math.Min(span1.Start, span2.Start);
            int end = Math.Max(span1.End, span2.End);
            return FromBounds(start, end);
        }

        /// <summary>
        /// 指定した位置がこのテキストスパンの範囲内にあるかどうか。
        /// </summary>
        /// <param name="position">判定する位置。</param>
        /// <returns>指定した位置が範囲内にある場合はtrue、それ以外の場合はfalse。</returns>
        public bool Contains(int position) => Start <= position && position < End;

        /// <summary>
        /// 指定したテキストスパンがこのテキストスパンの範囲内に完全に含まれているかどうか。
        /// </summary>
        /// <param name="other">判定するテキストスパン。</param>
        /// <returns>指定したテキストスパンが範囲内に完全に含まれている場合はtrue、それ以外の場合はfalse。</returns>
        public bool Contains(TextSpan other) => Start <= other.Start && other.End <= End;

        /// <summary>
        /// 指定したテキストスパンとこのテキストスパンが重なっているかどうか。
        /// 重なっているとは、両方のテキストスパンに共通の位置が存在することを意味する。
        /// 空のテキストスパンは、他のテキストスパンと重ならないとみなされる。
        /// </summary>
        /// <param name="other">判定するテキストスパン。</param>
        /// <returns>指定したテキストスパンが重なっている場合はtrue、それ以外の場合はfalse。</returns>
        public bool OverlapsWith(TextSpan other) => Math.Max(Start, other.Start) < Math.Min(End, other.End);

        /// <summary>
        /// 指定したテキストスパンとこのテキストスパンが交差しているかどうか。
        /// 交差しているとは、両方のテキストスパンに共通の位置が存在するか、または両方のテキストスパンの端点が一致することを意味する。
        /// 空のテキストスパンは、他のテキストスパンと交差するとみなされる。
        /// </summary>
        /// <param name="other">判定するテキストスパン。</param>
        /// <returns>指定したテキストスパンが交差している場合はtrue、それ以外の場合はfalse。</returns>
        public bool IntersectsWith(TextSpan other) => other.Start <= End && Start <= other.End;

        public override string ToString() => $"[{Start}..{End})";
    }
}
```

```PdxScriptAnalysis\Utilities\SyntaxTreePrinter.cs
using PdxScriptAnalysis.Syntax;
using System.Text;

namespace PdxScriptAnalysis.Utilities
{
    /// <summary>
    /// 構文木をツリー形式で出力するためのクラス。
    /// </summary>
    public class SyntaxTreePrinter : SyntaxWalker
    {
        private const string Indent = "    ";
        private const string BranchMiddle = "├───";
        private const string BranchLast = "└───";
        private const string Pipe = "│   ";
        private const string Empty = "    ";

        private readonly StringBuilder _builder = new();
        private int _depth = 0;
        private readonly Stack<bool> _isLastStack = new();


        /// <summary>
        /// ツリー形式で構文木を出力する静的メソッド。
        /// </summary>
        /// <param name="node">出力する構文木のルートノード。</param>
        /// <returns>ツリー形式の文字列。</returns>
        public static string Print(SyntaxNode node)
        {
            var printer = new SyntaxTreePrinter();
            printer.Visit(node);
            return printer._builder.ToString();
        }


        protected internal override void VisitRoot(RootNode node)
        {
            WriteLine(FormatNodeInfo(node));
            WriteChildren(node.ChildNodes());
        }

        protected internal override void VisitScalar(ScalarNode node)
        {
            WriteLine($"{node.GetType().Name} {node.Token.Text} {node.Span}");
        }

        protected internal override void VisitBlock(BlockNode node)
        {
            WriteLine(FormatNodeInfo(node));
            WriteChildren(node.ChildNodes());
        }

        protected internal override void VisitScalarProperty(ScalarPropertyNode node)
        {
            WriteLine(FormatNodeInfo(node));
            _depth++;
            WriteTokenLine("Key", node.Key);
            WriteTokenLine("Operator", node.Operator);
            WriteLastChild(node.Value);
            _depth--;
        }

        protected internal override void VisitBlockProperty(BlockPropertyNode node)
        {
            WriteLine(FormatNodeInfo(node));
            _depth++;
            WriteTokenLine("Key", node.Key);
            WriteTokenLine("Operator", node.Operator);
            WriteLastChild(node.Value);
            _depth--;
        }

        protected internal override void VisitTypedBlockProperty(TypedBlockPropertyNode node)
        {
            WriteLine(FormatNodeInfo(node));
            _depth++;
            WriteTokenLine("Key", node.Key);
            WriteTokenLine("Operator", node.Operator);
            WriteTokenLine("TypeQualifier", node.TypeQualifier);
            WriteLastChild(node.Value);
            _depth--;
        }


        private string BuildPrefix()
        {
            var prefixParts = _isLastStack
                .Reverse()
                .Select((isLast, index) => IsDirectParent(index) ? BuildBranch(isLast) : BuildPipe(isLast));
            return string.Concat(prefixParts);
        }

        private bool IsDirectParent(int depth)
            => depth == _depth - 1;
        private static string BuildBranch(bool isLast)
            => isLast ? BranchLast : BranchMiddle;
        private static string BuildPipe(bool isLast)
            => isLast ? Empty : Pipe;

        private void WriteLine(string content)
        {
            _builder.Append(BuildPrefix());
            _builder.AppendLine(content);
        }

        private void WriteTokenLine(string label, SyntaxToken token)
        {
            _builder.Append(BuildPrefix());
            _builder.Append(BranchMiddle);
            _builder.AppendLine($"{label}: {token.Kind} \"{token.Text}\"");
        }

        private void WriteLastChild(SyntaxNode child)
        {
            _isLastStack.Push(true);
            Visit(child);
            _isLastStack.Pop();
        }

        private void WriteChildren(IEnumerable<SyntaxNode> children)
        {
            var childList = children.ToList();
            _depth++;
            for (int i = 0; i < childList.Count; i++)
            {
                var isLast = (i == childList.Count - 1);
                _isLastStack.Push(isLast);
                Visit(childList[i]);
                _isLastStack.Pop();
            }
            _depth--;
        }

        private static string FormatNodeInfo(SyntaxNode node)
            => $"{node.GetType().Name} {node.Span}";
    }
}
```

```PdxScriptAnalysis.Tests\Lexing\LexerTests.cs
using PdxScriptAnalysis.Lexing;
using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Tests.Lexing
{
    public class LexerTests
    {
        private static IReadOnlyList<SyntaxToken> Tokenize(string text)
            => new Lexer(SourceText.From(text)).Tokenize();

        private static IReadOnlyList<SyntaxToken> TokenizeWithoutEOF(string text)
            => Tokenize(text).Where(t => !t.IsEndOfFile).ToList();

        [Theory(DisplayName = "必ず1文字である制御文字トークン")]
        [InlineData("{", SyntaxKind.LeftBrace)]
        [InlineData("}", SyntaxKind.RightBrace)]
        [InlineData("=", SyntaxKind.Equals)]
        public void SingleControlChar_IsRecognized(string input, SyntaxKind expected)
        {
            var tokens = TokenizeWithoutEOF(input);
            Assert.Single(tokens);
            Assert.Equal(expected, tokens[0].Kind);
            Assert.Equal(input, tokens[0].Text);
        }


        [Theory(DisplayName = "複合演算子の可能性があるトークン")]
        [InlineData("<=", SyntaxKind.LessThanEquals)]
        [InlineData(">=", SyntaxKind.GreaterThanEquals)]
        [InlineData("!=", SyntaxKind.NotEquals)]
        [InlineData("?=", SyntaxKind.QuestionEquals)]
        [InlineData("<", SyntaxKind.LessThan)]
        [InlineData(">", SyntaxKind.GreaterThan)]
        public void CompoundOperator_IsRecognized(string input, SyntaxKind expected)
        {
            var tokens = TokenizeWithoutEOF(input);
            Assert.Single(tokens);
            Assert.Equal(expected, tokens[0].Kind);
            Assert.Equal(input, tokens[0].Text);
        }

        [Theory(DisplayName = "不正な演算子")]
        [InlineData("!")]
        [InlineData("?")]
        public void InvalidOperator_IsUnknown(string input)
        {
            var tokens = TokenizeWithoutEOF(input);
            Assert.Single(tokens);
            Assert.Equal(SyntaxKind.Unknown, tokens[0].Kind);
            Assert.Equal(input, tokens[0].Text);
        }


        [Theory(DisplayName = "Atomトークン")]
        [InlineData("atom")]
        [InlineData("yes")]
        [InlineData("no")]
        [InlineData("c:JAP")]
        [InlineData("1842.1.t")]
        [InlineData("0.5")]
        [InlineData("-100")]
        public void Atom_IsRecognized(string input)
        {
            var tokens = TokenizeWithoutEOF(input);
            Assert.Single(tokens);
            Assert.Equal(SyntaxKind.Atom, tokens[0].Kind);
            Assert.Equal(input, tokens[0].Text);
        }

        [Fact(DisplayName = "Atomは制御文字で区切られる")]
        public void Atom_IsSeparatedByControlChars()
        {
            var input = "foo=bar{baz}";
            var tokens = TokenizeWithoutEOF(input);
            Assert.Equal(6, tokens.Count);
            Assert.Equal(SyntaxKind.Atom, tokens[0].Kind);
            Assert.Equal(SyntaxKind.Equals, tokens[1].Kind);
            Assert.Equal(SyntaxKind.Atom, tokens[2].Kind);
            Assert.Equal(SyntaxKind.LeftBrace, tokens[3].Kind);
            Assert.Equal(SyntaxKind.Atom, tokens[4].Kind);
            Assert.Equal(SyntaxKind.RightBrace, tokens[5].Kind);
            Assert.Equal("foo", tokens[0].Text);
            Assert.Equal("=", tokens[1].Text);
            Assert.Equal("bar", tokens[2].Text);
            Assert.Equal("{", tokens[3].Text);
            Assert.Equal("baz", tokens[4].Text);
            Assert.Equal("}", tokens[5].Text);
        }


        [Theory(DisplayName = "文字列リテラル")]
        [InlineData("\"Paradox\"")]
        [InlineData("\"String with spaces\"")]
        [InlineData("\"\"")] // 空文字列も有効
        public void StringLiteral_IsRecognized(string input)
        {
            var tokens = TokenizeWithoutEOF(input);
            Assert.Single(tokens);
            Assert.Equal(SyntaxKind.StringLiteral, tokens[0].Kind);
            Assert.Equal(input, tokens[0].Text);
        }

        [Fact(DisplayName = "文字列リテラルは閉じる必要がある")]
        public void StringLiteral_MustBeClosed()
        {
            var input = "\"Unclosed string";
            var tokens = TokenizeWithoutEOF(input);
            Assert.Single(tokens);
            Assert.Equal(SyntaxKind.Unknown, tokens[0].Kind);
            Assert.Equal(input, tokens[0].Text);
        }


        [Fact(DisplayName = "空白・改行は無視される")]
        public void Whitespace_IsIgnored()
        {
            var input = "  foo \t bar \n baz  ";
            var tokens = TokenizeWithoutEOF(input);
            Assert.Equal(3, tokens.Count);
            Assert.Equal("foo", tokens[0].Text);
            Assert.Equal("bar", tokens[1].Text);
            Assert.Equal("baz", tokens[2].Text);
        }

        [Fact(DisplayName = "コメントは行末まで無視される")]
        public void CommentLine_IsIgnored()
        {
            var input = "foo # this is a comment\nbar";
            var tokens = TokenizeWithoutEOF(input);
            Assert.Equal(2, tokens.Count);
            Assert.Equal("foo", tokens[0].Text);
            Assert.Equal("bar", tokens[1].Text);
        }


        [Fact(DisplayName = "空入力はEOFトークンのみ")]
        public void EmptyInput_ProducesOnlyEOF()
        {
            var input = "";
            var tokens = Tokenize(input);
            Assert.Single(tokens);
            Assert.Equal(SyntaxKind.EndOfFile, tokens[0].Kind);
        }

        [Fact(DisplayName = "最後のトークンはEOFである")]
        public void LastToken_IsEOF()
        {
            var input = "foo=bar";
            var tokens = Tokenize(input);
            Assert.True(tokens.Count > 0);
            Assert.Equal(SyntaxKind.EndOfFile, tokens[^1].Kind);
        }


        [Fact(DisplayName = "トークンスパンは正しく計算される")]
        public void TokenSpan_IsCalculatedCorrectly()
        {
            var input = "foo = bar";
            var tokens = Tokenize(input);
            Assert.Equal(4, tokens.Count);
            Assert.Equal(0, tokens[0].Span.Start);
            Assert.Equal(3, tokens[0].Span.Length);
            Assert.Equal(4, tokens[1].Span.Start);
            Assert.Equal(1, tokens[1].Span.Length);
            Assert.Equal(6, tokens[2].Span.Start);
            Assert.Equal(3, tokens[2].Span.Length);
            Assert.Equal(9, tokens[3].Span.Start);
            Assert.Equal(0, tokens[3].Span.Length);
        }
    }
}
```

```PdxScriptAnalysis.Tests\Parsing\ParserTests.cs
using PdxScriptAnalysis.Diagnostics;
using PdxScriptAnalysis.Syntax;

namespace PdxScriptAnalysis.Tests.Parsing
{
    public class ParserTests
    {
        private static RootNode Parse(string text)
            => ScriptTree.ParseText(text).Root;

        private static ScriptTree ParseToTree(string text)
            => ScriptTree.ParseText(text);

        private static T AssertSingleChild<T>(SyntaxNode node) where T : SyntaxNode
        {
            Assert.Single(node.ChildNodes());
            return Assert.IsType<T>(node.ChildNodes().First());
        }


        [Fact(DisplayName = "空入力は空のルートを生成する")]
        public void EmptyInput_ProducesEmptyRoot()
        {
            var root = Parse("");
            Assert.Empty(root.Children);
        }


        [Fact(DisplayName = "スカラープロパティの解析")]
        public void ScalarProperty_IsParsedCorrectly()
        {
            var root = Parse("key = value");
            var prop = AssertSingleChild<ScalarPropertyNode>(root);
            Assert.Equal("key", prop.Key.Text);
            Assert.Equal("=", prop.Operator.Text);
            var value = Assert.IsType<ScalarNode>(prop.Value);
            Assert.Equal("value", value.Token.Text);
        }

        [Fact(DisplayName = "文字列リテラルを持つスカラープロパティの解析")]
        public void ScalarProperty_WithStringLiteral_IsParsedCorrectly()
        {
            var root = Parse("key = \"string value\"");
            var prop = AssertSingleChild<ScalarPropertyNode>(root);
            Assert.Equal(SyntaxKind.StringLiteral, prop.Value.Token.Kind);
        }

        [Theory(DisplayName = "各種演算子を持つスカラープロパティの解析")]
        [InlineData("key = value", SyntaxKind.Equals)]
        [InlineData("key <= value", SyntaxKind.LessThanEquals)]
        [InlineData("key >= value", SyntaxKind.GreaterThanEquals)]
        [InlineData("key != value", SyntaxKind.NotEquals)]
        [InlineData("key ?= value", SyntaxKind.QuestionEquals)]
        [InlineData("key < value", SyntaxKind.LessThan)]
        [InlineData("key > value", SyntaxKind.GreaterThan)]
        public void ScalarProperty_WithVariousOperators_IsParsedCorrectly(string input, SyntaxKind expectedOperator)
        {
            var root = Parse(input);
            var prop = AssertSingleChild<ScalarPropertyNode>(root);
            Assert.Equal(expectedOperator, prop.Operator.Kind);
        }


        [Fact(DisplayName = "ブロックプロパティの解析")]
        public void BlockProperty_IsParsedCorrectly()
        {
            var root = Parse("block = { key = value foo = bar }");
            var prop = AssertSingleChild<BlockPropertyNode>(root);
            Assert.Equal("block", prop.Key.Text);
            Assert.Equal("=", prop.Operator.Text);
            var block = Assert.IsType<BlockNode>(prop.Value);
            Assert.Equal(2, block.Children.Count);
        }

        [Fact(DisplayName = "空のブロックプロパティの解析")]
        public void BlockProperty_EmptyBlock_IsParsedCorrectly()
        {
            var root = Parse("block = { }");
            var prop = AssertSingleChild<BlockPropertyNode>(root);
            var block = Assert.IsType<BlockNode>(prop.Value);
            Assert.Empty(block.Children);
        }

        [Fact(DisplayName = "ネストしたブロックプロパティの解析")]
        public void BlockProperty_NestedBlocks_IsParsedCorrectly()
        {
            var root = Parse("block = { innerBlock = { key = value } }");
            var prop = AssertSingleChild<BlockPropertyNode>(root);
            var innerProp = AssertSingleChild<BlockPropertyNode>(prop.Value);
            Assert.Equal("block", prop.Key.Text);
            Assert.Equal("innerBlock", innerProp.Key.Text);
            Assert.Single(innerProp.Value.Children);
        }


        [Fact(DisplayName = "修飾子付きブロックプロパティの解析")]
        public void TypedBlockProperty_IsParsedCorrectly()
        {
            var root = Parse("block = type { value1 value2 value3 }");
            var prop = AssertSingleChild<TypedBlockPropertyNode>(root);
            Assert.Equal("block", prop.Key.Text);
            Assert.Equal("=", prop.Operator.Text);
            Assert.Equal("type", prop.TypeQualifier.Text);
            Assert.Equal(3, prop.Value.Children.Count);
        }


        [Fact(DisplayName = "ブロックは単体のブロックをもつことができる")]
        public void Block_CanContainSingleBlock()
        {
            var root = Parse("block = { { key = value } }");
            var prop = AssertSingleChild<BlockPropertyNode>(root);
            var block = Assert.IsType<BlockNode>(prop.Value);
            Assert.Single(block.Children);
            var innerBlock = AssertSingleChild<BlockNode>(block);
            Assert.Single(innerBlock.Children);
        }


        [Fact(DisplayName = "閉じられていないブロックは診断を生成するが、ASTは生成される")]
        public void UnclosedBlock_ProducesDiagnosticAndAst()
        {
            var tree = ParseToTree("block = { key = value ");
            Assert.NotNull(tree.Root);

            var node = AssertSingleChild<BlockPropertyNode>(tree.Root);
            Assert.Equal("block", node.Key.Text);

            Assert.True(tree.HasErrorsOrWarnings);
            var diag = Assert.Single(tree.Diagnostics);
            Assert.Equal("Unexpected end of file. Expected '}' to close the block.", diag.Message);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal(22, diag.Span.Start);
            Assert.Equal(0, diag.Span.Length);
        }

        [Fact(DisplayName = "右辺が不正なプロパティは診断を生成するが、ASTは生成される")]
        public void MissingPropertyValue_ProducesDiagnosticAndAst()
        {
            var tree = ParseToTree("key = ");
            Assert.NotNull(tree.Root);
            var node = AssertSingleChild<ScalarPropertyNode>(tree.Root);
            Assert.Equal("key", node.Key.Text);
            Assert.Equal("=", node.Operator.Text);
            Assert.Equal(SyntaxKind.Unknown, node.Value.Token.Kind);
            Assert.True(tree.HasErrorsOrWarnings);
            var diag = Assert.Single(tree.Diagnostics);
            Assert.Equal("Invalid property value: \"\"", diag.Message);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal(6, diag.Span.Start);
            Assert.Equal(0, diag.Span.Length);
        }

        [Fact(DisplayName = "予期しないトークンは診断を生成するが、ASTは生成される")]
        public void UnexpectedToken_ProducesDiagnosticAndAst()
        {
            var tree = ParseToTree("}");
            Assert.NotNull(tree.Root);
            Assert.Empty(tree.Root.Children);
            Assert.True(tree.HasErrorsOrWarnings);
            var diag = Assert.Single(tree.Diagnostics);
            Assert.Equal("Unexpected token: \"}\"", diag.Message);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal(0, diag.Span.Start);
            Assert.Equal(1, diag.Span.Length);
        }

        [Fact(DisplayName = "複数のエラーが発生してもすべての診断が収集される")]
        public void MultipleErrors_AllDiagnosticsCollected()
        {
            var tree = ParseToTree("key = }\n block = { key2 = value2 ");
            Assert.NotNull(tree.Root);
            Assert.True(tree.HasErrorsOrWarnings);
            Assert.Equal(3, tree.Diagnostics.Count);

            var diag1 = tree.Diagnostics[0];
            Assert.Equal("Invalid property value: \"}\"", diag1.Message);
            Assert.Equal(DiagnosticSeverity.Error, diag1.Severity);
            Assert.Equal(6, diag1.Span.Start);
            Assert.Equal(1, diag1.Span.Length);

            var diag2 = tree.Diagnostics[1];
            Assert.Equal("Unexpected token: \"}\"", diag2.Message);
            Assert.Equal(DiagnosticSeverity.Error, diag2.Severity);
            Assert.Equal(6, diag2.Span.Start);
            Assert.Equal(1, diag2.Span.Length);

            var diag3 = tree.Diagnostics[2];
            Assert.Equal("Unexpected end of file. Expected '}' to close the block.", diag3.Message);
            Assert.Equal(DiagnosticSeverity.Error, diag3.Severity);
            Assert.Equal(33, diag3.Span.Start);
            Assert.Equal(0, diag3.Span.Length);
        }

        [Fact(DisplayName = "正しい構文の入力は診断を生成せず、正しいASTを生成する")]
        public void ValidInput_NoDiagnosticsAndCorrectAst()
        {
            var tree = ParseToTree("key = value\nblock = { innerKey = innerValue }");
            Assert.NotNull(tree.Root);
            Assert.False(tree.HasErrorsOrWarnings);
            Assert.Empty(tree.Diagnostics);
        }


        [Fact(DisplayName = "テキストスパンが正しく計算されている")]
        public void TextSpans_AreCalculatedCorrectly()
        {
            var root = Parse("key = value\nblock = { innerKey = innerValue }");
            Assert.Equal(0, root.Span.Start);
            Assert.Equal(45, root.Span.Length);
        }
    }
}
```

```PdxScriptAnalysis.Tests\Text\SourceTextTests.cs
using PdxScriptAnalysis.Text;

namespace PdxScriptAnalysis.Tests.Text
{
    public class SourceTextTests
    {
        [Fact(DisplayName = "1行目のLinePositionのLineの値は0であること")]
        public void GetLinePosition_FirstLine_ReturnsZero()
        {
            var source = SourceText.From("line1\nline2");
            var linePosition = source.GetLinePosition(0);
            Assert.Equal(0, linePosition.Line);
        }

        [Fact(DisplayName = "改行の後のLinePositionのLineの値は1であること")]
        public void GetLinePosition_AfterNewLine_ReturnsOne()
        {
            var source = SourceText.From("line1\nline2");
            var linePosition = source.GetLinePosition(6);
            Assert.Equal(1, linePosition.Line);
        }

        [Fact(DisplayName = "行の先頭のLinePositionのCharacterの値は0であること")]
        public void GetLinePosition_StartOfLine_ReturnsZero()
        {
            var source = SourceText.From("line1\nline2");
            var linePosition = source.GetLinePosition(0);
            Assert.Equal(0, linePosition.Character);
        }

        [Fact(DisplayName = "CRLFは合わせて改行として扱われること")]
        public void GetLinePosition_CRLF_ReturnsCorrectLine()
        {
            var source = SourceText.From("line1\r\nline2");
            var linePosition = source.GetLinePosition(7);
            Assert.Equal(1, linePosition.Line);
        }


        [Fact(DisplayName = "GetSubTextは指定したTextSpanに対応する部分文字列を返すこと")]
        public void GetSubText_ValidSpan_ReturnsSubstring()
        {
            var source = SourceText.From("Hello, World!");
            var span = new TextSpan(7, 5);
            var subText = source.GetSubText(span);
            Assert.Equal("World", subText);
        }

        [Fact(DisplayName = "GetSubTextは空のTextSpanに対して空文字列を返すこと")]
        public void GetSubText_EmptySpan_ReturnsEmptyString()
        {
            var source = SourceText.From("Hello, World!");
            var span = new TextSpan(5, 0);
            var subText = source.GetSubText(span);
            Assert.Equal(string.Empty, subText);
        }


        [Fact(DisplayName = "Fromはnullを受け入れないこと")]
        public void From_NullInput_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => SourceText.From(null!));
        }

        [Fact(DisplayName = "GetLinePositionは位置が範囲外の場合にArgumentOutOfRangeExceptionをスローすること")]
        public void GetLinePosition_OutOfRange_ThrowsArgumentOutOfRangeException()
        {
            var source = SourceText.From("line1\nline2");
            Assert.Throws<ArgumentOutOfRangeException>(() => source.GetLinePosition(100));
        }

        [Fact(DisplayName = "GetSubTextはTextSpanが範囲外の場合にArgumentOutOfRangeExceptionをスローすること")]
        public void GetSubText_OutOfRange_ThrowsArgumentOutOfRangeException()
        {
            var source = SourceText.From("Hello, World!");
            var span = new TextSpan(0, 100);
            Assert.Throws<ArgumentOutOfRangeException>(() => source.GetSubText(span));
        }

        [Fact(DisplayName = "GetPositionは行・列位置が範囲外の場合にArgumentOutOfRangeExceptionをスローすること")]
        public void GetPosition_OutOfRange_ThrowsArgumentOutOfRangeException()
        {
            var source = SourceText.From("line1");
            var linePosition = new LinePosition(0, 5);
            Assert.Throws<ArgumentOutOfRangeException>(() => source.GetPosition(linePosition));
        }
    }
}
```

```Victoria3.App\Program.cs
using System.CommandLine;
using Victoria3.App.Commands;

namespace Victoria3.App
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Victoria 3 データ解析ツール");

            rootCommand.Subcommands.Add(new InitCommand());
            rootCommand.Subcommands.Add(new ConfigCommand());
            rootCommand.Subcommands.Add(new ListCommand());
            rootCommand.Subcommands.Add(new ExportCommand());


            // コマンドライン引数が指定されていない場合、ユーザーに入力を促す
            if (args.Length <= 0)
            {
                string? input = null;
                while (input is null)
                {
                    Console.WriteLine("コマンドを入力してください");
                    input = Console.ReadLine();
                }
                args = input.Split(" ");
            }

            return await rootCommand.Parse(args).InvokeAsync();
        }
    }
}
```

```Victoria3.App\Commands\ConfigCommand.cs
using System.CommandLine;

namespace Victoria3.App.Commands
{
    internal class ConfigCommand : Command
    {
        internal ConfigCommand() : base("config", "ツールの設定を行います")
        {
            this.Subcommands.Add(new ConfigShowCommand());
            this.Subcommands.Add(new ConfigSetCommand());
        }
    }
}
```

```Victoria3.App\Commands\ConfigSetCommand.cs
using System.CommandLine;
using Tomlyn;
using Victoria3.App.Config;

namespace Victoria3.App.Commands
{
    internal class ConfigSetCommand : Command
    {
        internal ConfigSetCommand() : base("set", "設定項目を変更します")
        {
            var keyArgument = new Argument<string>("key")
            {
                Description = "設定項目のキー"
            };
            var valueArgument = new Argument<string>("value")
            {
                Description = "設定項目の値"
            };
            this.Arguments.Add(keyArgument);
            this.Arguments.Add(valueArgument);
            this.SetAction(parseResult =>
            {
                var key = parseResult.GetValue(keyArgument);
                var value = parseResult.GetValue(valueArgument);

                if (key is null)
                {
                    Console.WriteLine("設定項目のキーが指定されていません。");
                    return;
                }
                if (value is null)
                {
                    Console.WriteLine("設定項目の値が指定されていません。");
                    return;
                }

                Console.WriteLine($"設定項目 '{key}' を '{value}' に変更します...");

                var configPath = Path.Combine(Environment.CurrentDirectory, "vic3tool.toml");

                var config = new AppConfig();
                Console.WriteLine("設定ファイルのパス: " + configPath);
                if (File.Exists(configPath))
                {
                    var configText = File.ReadAllText(configPath);
                    config = TomlSerializer.Deserialize<AppConfig>(configText);
                }
                else
                {
                    Console.WriteLine("設定ファイルが見つかりません。");
                    return;
                }

                if (config is null)
                {
                    Console.WriteLine("設定ファイルの読み込みに失敗しました。");
                    return;
                }

                switch (key)
                {
                    case "game.directory":
                        config.Game.Directory = value;
                        break;
                    case "output.directory":
                        config.Output.Directory = value;
                        break;
                    default:
                        Console.WriteLine($"未知の設定項目: {key}");
                        return;
                }

                var text = TomlSerializer.Serialize(config);
                File.WriteAllText(configPath, text);
                Console.WriteLine("設定ファイルを更新しました。");
            });
        }
    }
}
```

```Victoria3.App\Commands\ConfigShowCommand.cs
using System.CommandLine;
using Tomlyn;
using Victoria3.App.Config;

namespace Victoria3.App.Commands
{
    internal class ConfigShowCommand : Command
    {
        internal ConfigShowCommand() : base("show", "現在の設定を表示します")
        {
            this.SetAction(parseResult =>
            {
                Console.WriteLine("現在の設定:");
                var configPath = Path.Combine(Environment.CurrentDirectory, "vic3tool.toml");
                Console.WriteLine("設定ファイルのパス: " + configPath);
                if (File.Exists(configPath))
                {
                    var configText = File.ReadAllText(configPath);
                    var config = TomlSerializer.Deserialize<AppConfig>(configText);
                    Console.WriteLine($"ゲームディレクトリ: {config?.Game?.Directory ?? "未設定"}");
                    Console.WriteLine($"出力ディレクトリ: {config?.Output?.Directory ?? "未設定"}");
                }
                else
                {
                    Console.WriteLine("設定ファイルが見つかりません。");
                }
            });
        }
    }
}
```

```Victoria3.App\Commands\ExportCommand.cs
using System.CommandLine;

namespace Victoria3.App.Commands
{
    internal class ExportCommand : Command
    {
        internal ExportCommand() : base("export", "指定したゲームデータをCSV形式でエクスポートします")
        {
            this.Subcommands.Add(new ExportCountriesCommand());
            this.Subcommands.Add(new ExportFormableCountriesCommand());
            this.Subcommands.Add(new ExportReleasableCountriesCommand());
            this.Subcommands.Add(new ExportHistoricalStateRegionCommand());
        }
    }
}
```

```Victoria3.App\Commands\ExportCountriesCommand.cs
using PdxScriptAnalysis;
using System.CommandLine;
using Tomlyn;
using Victoria3.App.Config;
using Victoria3.App.Options;
using Victoria3.Formatting;
using Victoria3.Formatting.PukiwikiFormatters;
using Victoria3.GameData;
using Victoria3.Loading;
using Victoria3.Loading.Loaders;
using Victoria3.Localization;

namespace Victoria3.App.Commands
{
    internal class ExportCountriesCommand : Command
    {
        internal ExportCountriesCommand() : base("countries", "ゲーム内の国のデータをエクスポートします")
        {
            var formatOption = new FormatOption();
            var languageOption = new LanguageOption();

            this.Options.Add(formatOption);
            this.Options.Add(languageOption);

            this.SetAction(async parseResult =>
            {
                var format = parseResult.GetValue(formatOption);
                var language = parseResult.GetValue(languageOption);

                Console.WriteLine($"国のデータを{format}形式でエクスポートしています...");

                // 設定ファイルの読み込み
                var configPath = Path.Combine(Environment.CurrentDirectory, "vic3tool.toml");
                var config = new AppConfig();
                if (File.Exists(configPath))
                {
                    var configText = File.ReadAllText(configPath);
                    config = TomlSerializer.Deserialize<AppConfig>(configText);
                }
                else
                {
                    Console.WriteLine("設定ファイルが見つかりません。");
                    Console.WriteLine("設定ファイルのパス: " + configPath);
                    return;
                }
                if (config is null)
                {
                    Console.WriteLine("設定ファイルの読み込みに失敗しました。");
                    return;
                }

                var gameDir = config.Game.Directory;
                var output = LoadCountries(gameDir);

                var localizationPath = Path.Combine(gameDir, LocalizationPaths.GetPath(language!));
                var localizer = FileLocalizer.FromDirectory(localizationPath);

                if (format == "csv")
                {
                    var text = CsvFormatter<Country>.Format(output.Values, localizer);
                    var outputDir = Path.Combine(Environment.CurrentDirectory, config.Output.Directory);
                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }
                    var outputPath = Path.Combine(outputDir, "countries.csv");
                    File.WriteAllText(outputPath, text);
                }
                else if (format == "pukiwiki")
                {
                    var historicalStateRegionsOutput = ExportHistoricalStateRegionCommand.LoadHistoricalStateRegions(gameDir);
                    var releasableCountriesOutput = ExportReleasableCountriesCommand.LoadReleasableCountries(gameDir);
                    var formableCountriesOutput = ExportFormableCountriesCommand.LoadFormableCountries(gameDir);

                    var englishLocalizationPath = Path.Combine(gameDir, LocalizationPaths.English);
                    var englishLocalizer = FileLocalizer.FromDirectory(englishLocalizationPath);

                    var formatter = new CountryPukiwikiFormatter();
                    var text = formatter.Format(output.Values, historicalStateRegionsOutput.Values, releasableCountriesOutput.Values, formableCountriesOutput.Values, localizer, englishLocalizer);

                    var outputDir = Path.Combine(Environment.CurrentDirectory, config.Output.Directory);
                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }

                    var outputPath = Path.Combine(outputDir, "countries.txt");
                    File.WriteAllText(outputPath, text);
                }
                else
                {
                    Console.WriteLine($"サポートされていないフォーマット: {format}");
                }
            });
        }

        internal static LoadOutput<Country> LoadCountries(string gameDir)
        {
            var countryDataPath = Path.Combine(gameDir, Victoria3Paths.CountryDefinitions);
            // 解析
            var scriptTrees = Directory.EnumerateFiles(countryDataPath, "*.txt").Select(ScriptTree.ParseFile).ToList();
            Console.WriteLine($"ファイル\"{countryDataPath}\"を解析しました。診断結果: {scriptTrees.Sum(st => st.Diagnostics.Count)}件");
            // ロード
            var output = new CountryLoader(scriptTrees).Load();
            Console.WriteLine($"読み込んだ国の数: {output.Values.Count}、診断結果: {output.Diagnostics.Count}件");
            foreach (var diagnostic in output.Diagnostics)
            {
                Console.WriteLine($"診断結果: {diagnostic.Message} at {diagnostic.LinePosition}");
            }
            return output;
        }
    }
}
```

```Victoria3.App\Commands\ExportFormableCountriesCommand.cs
using PdxScriptAnalysis;
using System.CommandLine;
using Tomlyn;
using Victoria3.App.Config;
using Victoria3.App.Options;
using Victoria3.Formatting;
using Victoria3.Formatting.PukiwikiFormatters;
using Victoria3.GameData;
using Victoria3.Loading;
using Victoria3.Loading.Loaders;
using Victoria3.Localization;

namespace Victoria3.App.Commands
{
    internal class ExportFormableCountriesCommand : Command
    {
        internal ExportFormableCountriesCommand() : base("formable-countries", "ゲーム内の形成可能な国のデータをエクスポートします")
        {
            var formatOption = new FormatOption();
            var languageOption = new LanguageOption();

            this.Options.Add(formatOption);
            this.Options.Add(languageOption);

            this.SetAction(async parseResult =>
            {
                var format = parseResult.GetValue(formatOption);
                var language = parseResult.GetValue(languageOption);

                Console.WriteLine($"解放可能国家のデータを{format}形式でエクスポートしています...");

                // 設定ファイルの読み込み
                var configPath = Path.Combine(Environment.CurrentDirectory, "vic3tool.toml");
                var config = new AppConfig();
                if (File.Exists(configPath))
                {
                    var configText = File.ReadAllText(configPath);
                    config = TomlSerializer.Deserialize<AppConfig>(configText);
                }
                else
                {
                    Console.WriteLine("設定ファイルが見つかりません。");
                    Console.WriteLine("設定ファイルのパス: " + configPath);
                    return;
                }
                if (config is null)
                {
                    Console.WriteLine("設定ファイルの読み込みに失敗しました。");
                    return;
                }

                // ゲームディレクトリとゲームデータのパス
                var gameDir = config.Game.Directory;
                var output = LoadFormableCountries(gameDir);

                var localizationPath = Path.Combine(gameDir, LocalizationPaths.GetPath(language!));
                var localizer = FileLocalizer.FromDirectory(localizationPath);

                if (format == "csv")
                {
                    var text = CsvFormatter<FormableCountry>.Format(output.Values, localizer);
                    var outputDir = Path.Combine(Environment.CurrentDirectory, config.Output.Directory);
                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }
                    var outputPath = Path.Combine(outputDir, "formable_countries.csv");
                    File.WriteAllText(outputPath, text);
                }
                else if (format == "pukiwiki")
                {
                    var englishLocalizationPath = Path.Combine(gameDir, LocalizationPaths.English);
                    var englishLocalizer = FileLocalizer.FromDirectory(englishLocalizationPath);

                    var formatter = new FormableCountryPukiwikiFormatter();
                    var text = formatter.Format(output.Values, localizer);

                    var outputDir = Path.Combine(Environment.CurrentDirectory, config.Output.Directory);
                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }

                    var outputPath = Path.Combine(outputDir, "formable_countries.txt");
                    File.WriteAllText(outputPath, text);
                }
                else
                {
                    Console.WriteLine($"サポートされていないフォーマット: {format}");
                }
            });
        }

        internal static LoadOutput<FormableCountry> LoadFormableCountries(string gameDir)
        {
            var countryDataPath = Path.Combine(gameDir, Victoria3Paths.CountryFormation);
            // 解析
            var scriptTrees = Directory.EnumerateFiles(countryDataPath, "*.txt").Select(ScriptTree.ParseFile).ToList();
            Console.WriteLine($"ファイル\"{countryDataPath}\"を解析しました。診断結果: {scriptTrees.Sum(st => st.Diagnostics.Count)}件");
            // ロード
            var output = new FormableCountryLoader(scriptTrees).Load();
            Console.WriteLine($"読み込んだ形成可能国家の数: {output.Values.Count}、診断結果: {output.Diagnostics.Count}件");
            foreach (var diagnostic in output.Diagnostics)
            {
                Console.WriteLine($"診断結果: {diagnostic.Message} at {diagnostic.LinePosition}");
            }
            return output;
        }
    }
}
```

```Victoria3.App\Commands\ExportHistoricalStateRegionCommand.cs
using PdxScriptAnalysis;
using System.CommandLine;
using Tomlyn;
using Victoria3.App.Config;
using Victoria3.App.Options;
using Victoria3.Formatting;
using Victoria3.Formatting.PukiwikiFormatters;
using Victoria3.GameData;
using Victoria3.Loading;
using Victoria3.Loading.Loaders;
using Victoria3.Localization;

namespace Victoria3.App.Commands
{
    internal class ExportHistoricalStateRegionCommand : Command
    {
        internal ExportHistoricalStateRegionCommand() : base("historical-state-region", "ゲーム内の歴史的州地域のデータをエクスポートします")
        {
            var formatOption = new FormatOption();
            var languageOption = new LanguageOption();

            this.Options.Add(formatOption);
            this.Options.Add(languageOption);

            this.SetAction(async parseResult =>
            {
                var format = parseResult.GetValue(formatOption);
                var language = parseResult.GetValue(languageOption);

                Console.WriteLine($"歴史的州地域のデータを{format}形式でエクスポートしています...");

                // 設定ファイルの読み込み
                var configPath = Path.Combine(Environment.CurrentDirectory, "vic3tool.toml");
                var config = new AppConfig();
                if (File.Exists(configPath))
                {
                    var configText = File.ReadAllText(configPath);
                    config = TomlSerializer.Deserialize<AppConfig>(configText);
                }
                else
                {
                    Console.WriteLine("設定ファイルが見つかりません。");
                    Console.WriteLine("設定ファイルのパス: " + configPath);
                    return;
                }
                if (config is null)
                {
                    Console.WriteLine("設定ファイルの読み込みに失敗しました。");
                    return;
                }

                var gameDir = config.Game.Directory;
                var output = LoadHistoricalStateRegions(gameDir);

                var localizationPath = Path.Combine(gameDir, LocalizationPaths.GetPath(language!));
                var localizer = FileLocalizer.FromDirectory(localizationPath);

                if (format == "csv")
                {
                    var text = CsvFormatter<HistoricalStateRegion>.Format(output.Values, localizer);
                    var outputDir = Path.Combine(Environment.CurrentDirectory, config.Output.Directory);
                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }
                    var outputPath = Path.Combine(outputDir, "historical_state_regions.csv");
                    File.WriteAllText(outputPath, text);
                }
                else if (format == "pukiwiki")
                {
                    var formatter = new HistoricalStateRegionPukiwikiFormatter();
                    var text = formatter.Format(output.Values, localizer);

                    var outputDir = Path.Combine(Environment.CurrentDirectory, config.Output.Directory);
                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }

                    var outputPath = Path.Combine(outputDir, "historical_state_regions.txt");
                    File.WriteAllText(outputPath, text);
                }
                else
                {
                    Console.WriteLine($"サポートされていないフォーマット: {format}");
                }
            });
        }

        internal static LoadOutput<HistoricalStateRegion> LoadHistoricalStateRegions(string gameDir)
        {
            var historicalStatesDataPath = Path.Combine(gameDir, Victoria3Paths.HistoricalStates);
            // 解析
            var scriptTrees = Directory.EnumerateFiles(historicalStatesDataPath, "*.txt").Select(ScriptTree.ParseFile).ToList();
            Console.WriteLine($"ファイル\"{historicalStatesDataPath}\"を解析しました。診断結果: {scriptTrees.Sum(st => st.Diagnostics.Count)}件");
            // ロード
            var output = new HistoricalStateRegionLoader(scriptTrees).Load();
            Console.WriteLine($"読み込んだ歴史的州地域の数: {output.Values.Count}、診断結果: {output.Diagnostics.Count}件");
            foreach (var diagnostic in output.Diagnostics)
            {
                Console.WriteLine($"診断結果: {diagnostic.Message} at {diagnostic.LinePosition}");
            }
            return output;
        }
    }
}
```

```Victoria3.App\Commands\ExportReleasableCountriesCommand.cs
using PdxScriptAnalysis;
using System.CommandLine;
using Tomlyn;
using Victoria3.App.Config;
using Victoria3.App.Options;
using Victoria3.Formatting;
using Victoria3.Formatting.PukiwikiFormatters;
using Victoria3.GameData;
using Victoria3.Loading;
using Victoria3.Loading.Loaders;
using Victoria3.Localization;

namespace Victoria3.App.Commands
{
    internal class ExportReleasableCountriesCommand : Command
    {
        internal ExportReleasableCountriesCommand() : base("releasable-countries", "ゲーム内の解放可能な国のデータをエクスポートします")
        {
            var formatOption = new FormatOption();
            var languageOption = new LanguageOption();

            this.Options.Add(formatOption);
            this.Options.Add(languageOption);

            this.SetAction(async parseResult =>
            {
                var format = parseResult.GetValue(formatOption);
                var language = parseResult.GetValue(languageOption);

                Console.WriteLine($"解放可能国家のデータを{format}形式でエクスポートしています...");

                // 設定ファイルの読み込み
                var configPath = Path.Combine(Environment.CurrentDirectory, "vic3tool.toml");
                var config = new AppConfig();
                if (File.Exists(configPath))
                {
                    var configText = File.ReadAllText(configPath);
                    config = TomlSerializer.Deserialize<AppConfig>(configText);
                }
                else
                {
                    Console.WriteLine("設定ファイルが見つかりません。");
                    Console.WriteLine("設定ファイルのパス: " + configPath);
                    return;
                }
                if (config is null)
                {
                    Console.WriteLine("設定ファイルの読み込みに失敗しました。");
                    return;
                }

                // ゲームディレクトリとゲームデータのパス
                var gameDir = config.Game.Directory;
                var output = LoadReleasableCountries(gameDir);

                var localizationPath = Path.Combine(gameDir, LocalizationPaths.GetPath(language!));
                var localizer = FileLocalizer.FromDirectory(localizationPath);

                if (format == "csv")
                {
                    var text = CsvFormatter<ReleasableCountry>.Format(output.Values, localizer);
                    var outputDir = Path.Combine(Environment.CurrentDirectory, config.Output.Directory);
                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }
                    var outputPath = Path.Combine(outputDir, "releasable_countries.csv");
                    File.WriteAllText(outputPath, text);
                }
                else if (format == "pukiwiki")
                {
                    var englishLocalizationPath = Path.Combine(gameDir, LocalizationPaths.English);
                    var englishLocalizer = FileLocalizer.FromDirectory(englishLocalizationPath);

                    var formatter = new ReleasableCountryPukiwikiFormatter();
                    var text = formatter.Format(output.Values, localizer);

                    var outputDir = Path.Combine(Environment.CurrentDirectory, config.Output.Directory);
                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }

                    var outputPath = Path.Combine(outputDir, "releasable_countries.txt");
                    File.WriteAllText(outputPath, text);
                }
                else
                {
                    Console.WriteLine($"サポートされていないフォーマット: {format}");
                }
            });
        }

        internal static LoadOutput<ReleasableCountry> LoadReleasableCountries(string gameDir)
        {
            var countryDataPath = Path.Combine(gameDir, Victoria3Paths.CountryCreation);
            // 解析
            var scriptTrees = Directory.EnumerateFiles(countryDataPath, "*.txt").Select(ScriptTree.ParseFile).ToList();
            Console.WriteLine($"ファイル\"{countryDataPath}\"を解析しました。診断結果: {scriptTrees.Sum(st => st.Diagnostics.Count)}件");
            // ロード
            var output = new ReleasableCountryLoader(scriptTrees).Load();
            Console.WriteLine($"読み込んだ解放可能国家の数: {output.Values.Count}、診断結果: {output.Diagnostics.Count}件");
            foreach (var diagnostic in output.Diagnostics)
            {
                Console.WriteLine($"診断結果: {diagnostic.Message} at {diagnostic.LinePosition}");
            }
            return output;
        }
    }
}
```

```Victoria3.App\Commands\InitCommand.cs
using System.CommandLine;
using Tomlyn;
using Victoria3.App.Config;

namespace Victoria3.App.Commands
{
    internal class InitCommand : Command
    {
        internal InitCommand() : base("init", "設定の初期化を行います")
        {
            this.SetAction(parseResult =>
            {
                Console.WriteLine("設定を初期化しています...");
                var configPath = Path.Combine(Environment.CurrentDirectory, "vic3tool.toml");
                var config = new AppConfig()
                {
                    Game = new GameConfig(),
                    Output = new OutputConfig()
                };
                var text = TomlSerializer.Serialize(config);
                Console.WriteLine("設定ファイルのパス: " + configPath);
                File.WriteAllText(configPath, text);
                Console.WriteLine("設定ファイルを初期化しました。");
            });
        }
    }
}
```

```Victoria3.App\Commands\ListCommand.cs
using System.CommandLine;

namespace Victoria3.App.Commands
{
    internal class ListCommand : Command
    {
        internal ListCommand() : base("list", "指定したゲームデータの一覧を表示します")
        {
            this.Subcommands.Add(new ListCountriesCommand());
        }
    }
}
```

```Victoria3.App\Commands\ListCountriesCommand.cs
using PdxScriptAnalysis;
using System.CommandLine;
using Tomlyn;
using Victoria3.App.Config;
using Victoria3.Loading;
using Victoria3.Loading.Loaders;
using Victoria3.Localization;

namespace Victoria3.App.Commands
{
    internal class ListCountriesCommand : Command
    {
        internal ListCountriesCommand() : base("countries", "ゲーム内の国の一覧を表示します")
        {
            this.SetAction(async parseResult =>
            {
                Console.WriteLine("国の一覧を表示しています...");

                var configPath = Path.Combine(Environment.CurrentDirectory, "vic3tool.toml");

                if (!File.Exists(configPath))
                {
                    Console.WriteLine("設定ファイルが見つかりません。");
                    Console.WriteLine("設定ファイルのパス: " + configPath);
                    return;
                }

                var config = new AppConfig();
                var configText = File.ReadAllText(configPath);
                config = TomlSerializer.Deserialize<AppConfig>(configText);

                if (config is null)
                {
                    Console.WriteLine("設定ファイルの読み込みに失敗しました。");
                    return;
                }

                var gameDir = config.Game.Directory;

                var countryDataPath = Path.Combine(gameDir, Victoria3Paths.CountryDefinitions);

                var scriptTrees = Directory.EnumerateFiles(countryDataPath, "*.txt")
                    .Select(ScriptTree.ParseFile)
                    .ToList();

                Console.WriteLine($"ディレクトリ\"{countryDataPath}\"のファイルを解析しました。\n診断件数: {scriptTrees.Sum(st => st.Diagnostics.Count)}件");

                var output = new CountryLoader(scriptTrees).Load();

                Console.WriteLine($"{output.Values.Count}の国を読み込みました。\n診断件数: {output.Diagnostics.Count}件");
                var localizationPath = Path.Combine(gameDir, LocalizationPaths.Japanese);
                var localizer = FileLocalizer.FromDirectory(localizationPath);

                foreach (var (index, country) in output.Values.Index())
                {
                    Console.WriteLine($"{index + 1,-4}: タグ: {country.Tag}, 種別: {country.Type, -13}, ティア: {country.Tier,-17}, 名前: {localizer.Localize(country.Tag)}");
                }

                foreach (var diagnostic in output.Diagnostics)
                {
                    Console.WriteLine($"診断結果: {diagnostic}");
                }
            });
        }
    }
}
```

```Victoria3.App\Config\AppConfig.cs
namespace Victoria3.App.Config
{
    public sealed class AppConfig
    {
        public GameConfig Game { get; set; } = new();
        public OutputConfig Output { get; set; } = new();
    }
}
```

```Victoria3.App\Config\GameConfig.cs
namespace Victoria3.App.Config
{
    public class GameConfig
    {
        public string Directory { get; set; } = @"C:\Program Files (x86)\Steam\steamapps\common\Victoria 3\game";
    }
}
```

```Victoria3.App\Config\OutputConfig.cs
namespace Victoria3.App.Config
{
    public class OutputConfig
    {
        public string Directory { get; set; } = @".\output";
    }
}
```

```Victoria3.App\Options\FormatOption.cs
using System.CommandLine;

namespace Victoria3.App.Options
{
    internal class FormatOption : Option<string>
    {
        internal FormatOption() : base("--format", "-f")
        {
            Description = "エクスポートするフォーマット。現在は\"csv\"と\"pukiwiki\"のみサポートされています。";
            DefaultValueFactory = _ => "pukiwiki";
        }
    }
}
```

```Victoria3.App\Options\LanguageOption.cs
using System.CommandLine;

namespace Victoria3.App.Options
{
    internal class LanguageOption : Option<string>
    {
        internal LanguageOption() : base("--language", "-l")
        {
            Description = "エクスポートするローカライズの言語。現在は\"japanese\"と\"english\"のみサポートされています。";
            DefaultValueFactory = _ => "japanese";
        }
    }
}
```

```Victoria3.Formatting\CsvFormatter.cs
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
```

```Victoria3.Formatting\PukiwikiFormatters\CountryPukiwikiFormatter.cs
using System.Collections.Frozen;
using System.Text;
using Victoria3.GameData;
using Victoria3.Localization;

namespace Victoria3.Formatting.PukiwikiFormatters
{
    public sealed class CountryPukiwikiFormatter()
    {
        public string Format(
            IEnumerable<Country> items,
            IEnumerable<HistoricalStateRegion> historicalStateRegions,
            IEnumerable<ReleasableCountry> releasableCountries,
            IEnumerable<FormableCountry> formableCountries,
            ILocalizer localizer,
            ILocalizer englishLocalizer)
        {
            var sb = new StringBuilder();

            sb.AppendLine("|||||50|65|||||35|35|35|c");
            sb.AppendLine("||タグ|>|国名|種別|ティア|主要文化|国教((第一主要文化の文化宗教と異なる場合のみ記載))|ヒエラルキー|首都|初期存在|解放可能|形成可能|h");

            var initialTags = historicalStateRegions
                .SelectMany(hsr => hsr.CreateStates)
                .Select(cs => RemovePrefix(cs.Country))
                .ToFrozenSet();
            var releasableTags = releasableCountries.Select(rc => rc.Tag).ToFrozenSet();
            var formableTags = formableCountries.Select(fc => fc.Tag).ToFrozenSet();
            foreach (var country in items)
            {
                var englishName = englishLocalizer.Localize(country.Tag);
                var name = localizer.Localize(country.Tag);
                var countryType = localizer.Localize(country.Type.ToLocalizationKey());
                var tier = localizer.Localize(country.Tier.ToLocalizationKey());
                var cultures = string.Join("&br;", country.Cultures.Select(c => localizer.Localize(c)));
                var religion = localizer.Localize(country.Religion);
                var hierarchy = localizer.Localize(country.SocialHierarchy);
                var capital = localizer.Localize(country.Capital);
                var isInitial = initialTags.Contains(country.Tag) ? "初期" : "";
                var isReleasable = releasableTags.Contains(country.Tag) ? "解放" : "";
                var isFormable = formableTags.Contains(country.Tag) ? "形成" : "";

                sb.AppendLine($"|BGCOLOR({country.Color.ToColorCode()}):|~{country.Tag}|{englishName}|{name}|{countryType}|{tier}|{cultures}|{religion}|{hierarchy}|{capital}|{isInitial}|{isReleasable}|{isFormable}|");
            }
            return sb.ToString();
        }
        private static string RemovePrefix(string key)
        {
            var index = key.IndexOf(':');
            if (index >= 0)
            {
                return key[(index + 1)..];
            }
            return key;
        }
    }
}
```

```Victoria3.Formatting\PukiwikiFormatters\FormableCountryPukiwikiFormatter.cs
using System.Text;
using Victoria3.GameData;
using Victoria3.Localization;

namespace Victoria3.Formatting.PukiwikiFormatters
{
    public class FormableCountryPukiwikiFormatter
    {
        public string Format(
            IEnumerable<FormableCountry> items,
            ILocalizer localizer)
        {
            var sb = new StringBuilder();

            sb.AppendLine("|CENTER:|LEFT:||130|150||||c");
            sb.AppendLine("|~ |~国名|タグ|主要文化|条件|必要州|国家ティア|備考|h");

            foreach (var formableCountry in items)
            {
                var name = localizer.Localize(formableCountry.Tag);

                sb.AppendLine($"||~{name}|{formableCountry.Tag}||||||");
            }
            return sb.ToString();
        }
    }
}
```

```Victoria3.Formatting\PukiwikiFormatters\HistoricalStateRegionPukiwikiFormatter.cs
using System.Text;
using Victoria3.GameData;
using Victoria3.Localization;

namespace Victoria3.Formatting.PukiwikiFormatters
{
    public class HistoricalStateRegionPukiwikiFormatter
    {
        public string Format(
            IEnumerable<HistoricalStateRegion> items,
            ILocalizer localizer)
        {
            var sb = new StringBuilder();

            sb.AppendLine("|~州地域名|所有者|母国|請求権|h");

            foreach (var stateRegion in items)
            {
                var name = localizer.Localize(stateRegion.Tag);
                var countries = string.Join(", ", stateRegion.CreateStates.Select(cs => localizer.Localize(cs.Country)));
                var homelands = string.Join(", ", stateRegion.Homelands.Select(h => localizer.Localize(h)));
                var claims = string.Join(", ", stateRegion.Claims.Select(c => localizer.Localize(c)));

                sb.AppendLine($"|{name}|{countries}|{homelands}|{claims}|");
            }
            return sb.ToString();
        }
    }
}
```

```Victoria3.Formatting\PukiwikiFormatters\ReleasableCountryPukiwikiFormatter.cs
using System.Text;
using Victoria3.GameData;
using Victoria3.Localization;

namespace Victoria3.Formatting.PukiwikiFormatters
{
    public class ReleasableCountryPukiwikiFormatter
    {
        public string Format(
            IEnumerable<ReleasableCountry> items,
            ILocalizer localizer)
        {
            var sb = new StringBuilder();

            sb.AppendLine("|~ |~国名|タグ|h");

            foreach (var country in items)
            {
                var name = localizer.Localize(country.Tag);

                sb.AppendLine($"||~{name}|{country.Tag}|");
            }
            return sb.ToString();
        }
    }
}
```

```Victoria3.GameData\Country.cs
namespace Victoria3.GameData
{
    /// <summary>
    /// 国家を表すレコード。
    /// </summary>
    /// <param name="Tag">国家のタグ。</param>
    /// <param name="Color">国家の色。</param>
    /// <param name="Type">国家のタイプ。</param>
    /// <param name="Tier">国家のティア。</param>
    /// <param name="SocialHierarchy">国家の社会階層。</param>
    /// <param name="Religion">国家の宗教。</param>
    /// <param name="Cultures">国家の文化。</param>
    /// <param name="Capital">国家の首都。</param>
    /// <param name="IsNamedFromCapital">首都から名前が付けられているかどうか。</param>
    /// <param name="ValidAsHomeCountryForSeparatists">分離主義者の本国として有効かどうか。</param>
    /// <param name="PrimaryUnitColor">主要ユニットの色。</param>
    /// <param name="SecondaryUnitColor">二次ユニットの色。</param>
    /// <param name="TertiaryUnitColor">三次ユニットの色。</param>
    public sealed record Country(
        string Tag,
        GameColor Color,
        CountryType Type,
        CountryTier Tier,
        string? SocialHierarchy,
        string? Religion,
        IReadOnlyList<string> Cultures,
        string? Capital,
        bool IsNamedFromCapital,
        object? ValidAsHomeCountryForSeparatists,
        GameColor? PrimaryUnitColor,
        GameColor? SecondaryUnitColor,
        GameColor? TertiaryUnitColor)
        : IPropertySchemaProvider<Country>
    {
        private static readonly PropertySchema<Country>[] _propertySchemas =
        [
            new PropertySchema<Country>(typeof(string), "Tag", c => c.Tag),
            new PropertySchema<Country>(typeof(GameColor), "Color", c => c.Color),
            new PropertySchema<Country>(typeof(CountryType), "Type", c => c.Type, c => c.Type.ToLocalizationKey()),
            new PropertySchema<Country>(typeof(CountryTier), "Tier", c => c.Tier, c => c.Tier.ToLocalizationKey()),
            new PropertySchema<Country>(typeof(string), "Social Hierarchy", c => c.SocialHierarchy),
            new PropertySchema<Country>(typeof(string), "Religion", c => c.Religion),
            new PropertySchema<Country>(typeof(IReadOnlyList<string>), "Cultures", c => c.Cultures),
            new PropertySchema<Country>(typeof(string), "Capital", c => c.Capital),
            new PropertySchema<Country>(typeof(bool), "Is Named From Capital", c => c.IsNamedFromCapital),
            new PropertySchema<Country>(typeof(object), "Valid As Home Country For Separatists", c => c.ValidAsHomeCountryForSeparatists),
            new PropertySchema<Country>(typeof(GameColor?), "Primary Unit Color", c => c.PrimaryUnitColor),
            new PropertySchema<Country>(typeof(GameColor?), "Secondary Unit Color", c => c.SecondaryUnitColor),
            new PropertySchema<Country>(typeof(GameColor?), "Tertiary Unit Color", c => c.TertiaryUnitColor),
        ];

        /// <inheritdoc/>
        public static PropertySchema<Country>[] PropertySchemas
            => _propertySchemas;
    }
}
```

```Victoria3.GameData\CountryTier.cs
namespace Victoria3.GameData
{
    /// <summary>
    /// 国家のティアを表す列挙型。
    /// </summary>
    public enum CountryTier
    {
        Hegemony,
        Empire,
        Kingdom,
        GrandPrincipality,
        Principality,
        CityState,
    }

    public static class CountryTierExtensions
    {
        /// <summary>
        /// 国家のティアをローカライズキーに変換する拡張メソッド。
        /// </summary>
        /// <param name="tier">変換する国家のティア。</param>
        /// <returns>国家のティアに対応するローカライズキー。</returns>
        /// <exception cref="ArgumentOutOfRangeException">予期しない国家のティアが指定された場合にスローされる。</exception>
        public static string ToLocalizationKey(this CountryTier tier)
            => tier switch
            {
                CountryTier.Hegemony => "country_tier_hegemony",
                CountryTier.Empire => "country_tier_empire",
                CountryTier.Kingdom => "country_tier_kingdom",
                CountryTier.GrandPrincipality => "country_tier_grand_principality",
                CountryTier.Principality => "country_tier_principality",
                CountryTier.CityState => "country_tier_city_state",
                _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unexpected country tier")
            };

        /// <summary>
        /// 国家のティアの表示名を取得する拡張メソッド。
        /// </summary>
        /// <param name="tier">取得する国家のティア。</param>
        /// <returns>国家のティアに対応する表示名。</returns>
        /// <exception cref="ArgumentOutOfRangeException">予期しない国家のティアが指定された場合にスローされる。</exception>
        public static string ToDisplayName(this CountryTier tier)
            => tier switch
            {
                CountryTier.Hegemony => "Hegemony",
                CountryTier.Empire => "Empire",
                CountryTier.Kingdom => "Kingdom",
                CountryTier.GrandPrincipality => "Grand Principality",
                CountryTier.Principality => "Principality",
                CountryTier.CityState => "City State",
                _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unexpected country tier")
            };
    }
}
```

```Victoria3.GameData\CountryType.cs
namespace Victoria3.GameData
{
    /// <summary>
    /// 国家のタイプを表す列挙型。
    /// </summary>
    public enum CountryType
    {
        Recognized,
        Colonial,
        Unrecognized,
        Decentralized,
    }

    public static class CountryTypeExtensions
    {
        /// <summary>
        /// 国家の種別をローカライズキーに変換する拡張メソッド。
        /// </summary>
        /// <param name="type">変換する国家のタイプ。</param>
        /// <returns>国家のタイプに対応するローカライズキー。</returns>
        /// <exception cref="ArgumentOutOfRangeException">予期しない国家のタイプが指定された場合にスローされる。</exception>
        public static string ToLocalizationKey(this CountryType type)
            => type switch
            {
                CountryType.Recognized => "recognized",
                CountryType.Colonial => "colonial",
                CountryType.Unrecognized => "unrecognized",
                CountryType.Decentralized => "decentralized",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unexpected country type")
            };

        /// <summary>
        /// 国家の種別の表示名を取得する拡張メソッド。
        /// </summary>
        /// <param name="type">取得する国家のタイプ。</param>
        /// <returns>国家のタイプに対応する表示名。</returns>
        /// <exception cref="ArgumentOutOfRangeException">予期しない国家のタイプが指定された場合にスローされる。</exception>
        public static string ToDisplayName(this CountryType type)
            => type switch
            {
                CountryType.Recognized => "Recognized",
                CountryType.Colonial => "Colonial",
                CountryType.Unrecognized => "Unrecognized",
                CountryType.Decentralized => "Decentralized",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unexpected country type")
            };
    }
}
```

```Victoria3.GameData\CreateState.cs
namespace Victoria3.GameData
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Country"></param>
    /// <param name="StateType"></param>
    /// <param name="Provinces"></param>
    public sealed record CreateState(
        string Country,
        string? StateType,
        IReadOnlyList<string> Provinces)
    {
        public override string ToString()
            => $"{Country}({Provinces.Count})";
    }
}
```

```Victoria3.GameData\FormableCountry.cs
namespace Victoria3.GameData
{
    /// <summary>
    /// 形成可能国家を表すレコード。形成可能国家は、特定の条件を満たすことでゲーム内で形成されることができる国家を表す。
    /// </summary>
    /// <param name="Tag">国家のタグ。</param>
    /// <param name="States">必要な州のリスト。</param>
    /// <param name="UseCultureStates">必要な州として文化に基づく州を使用するかどうか。</param>
    /// <param name="RequiredStatesFraction">必要な州の割合。</param>
    /// <param name="AIWillDo">AIが実行するかどうか。</param>
    /// <param name="Potential">形成の潜在条件。</param>
    /// <param name="Possible">形成の発動条件。</param>
    /// <param name="GeographicRegion">地理的な地域。</param>
    /// <param name="IsMajorFormation">大国統一かどうか。</param>
    /// <param name="UnificationPlay">統一外交戦の情報。</param>
    /// <param name="LeadershipPlay">リーダーシップ外交戦の情報。</param>
    /// <param name="MaxNumFormationCandidates">統一候補の最大数。</param>
    /// <param name="CanBeFormationCandidate">統一候補になれるかどうか。</param>
    /// <param name="CanBeUnificationTarget">統一の対象になれるかどうか。</param>
    public sealed record FormableCountry(
        string Tag,
        IReadOnlyList<string> States,
        bool UseCultureStates,
        decimal RequiredStatesFraction,
        object? AIWillDo,
        object? Potential,
        object? Possible,
        string? GeographicRegion,
        bool IsMajorFormation,
        string? UnificationPlay,
        string? LeadershipPlay,
        int? MaxNumFormationCandidates,
        object? CanBeFormationCandidate,
        object? CanBeUnificationTarget)
        : IPropertySchemaProvider<FormableCountry>
    {
        private static readonly PropertySchema<FormableCountry>[] _propertySchemas =
        [
            new PropertySchema<FormableCountry>(typeof(string), "Tag", c => c.Tag),
            new PropertySchema<FormableCountry>(typeof(IReadOnlyList<string>), "States", c => c.States),
            new PropertySchema<FormableCountry>(typeof(bool), "Use Culture States", c => c.UseCultureStates),
            new PropertySchema<FormableCountry>(typeof(decimal), "Required States Fraction", c => c.RequiredStatesFraction),
            new PropertySchema<FormableCountry>(typeof(object), "AI Will Do", c => c.AIWillDo),
            new PropertySchema<FormableCountry>(typeof(object), "Potential", c => c.Potential),
            new PropertySchema<FormableCountry>(typeof(object), "Possible", c => c.Possible),
            new PropertySchema<FormableCountry>(typeof(string), "Geographic Region", c => c.GeographicRegion),
            new PropertySchema<FormableCountry>(typeof(bool), "Is Major Formation", c => c.IsMajorFormation),
            new PropertySchema<FormableCountry>(typeof(string), "Unification Play", c => c.UnificationPlay),
            new PropertySchema<FormableCountry>(typeof(string), "Leadership Play", c => c.LeadershipPlay),
            new PropertySchema<FormableCountry>(typeof(decimal), "Max Num Formation Candidates", c => c.MaxNumFormationCandidates),
            new PropertySchema<FormableCountry>(typeof(object), "Can Be Formation Candidate", c => c.CanBeFormationCandidate),
            new PropertySchema<FormableCountry>(typeof(object), "Can Be Unification Target", c => c.CanBeUnificationTarget),
        ];

        /// <inheritdoc/>
        public static PropertySchema<FormableCountry>[] PropertySchemas
            => _propertySchemas;
    }
}
```

```Victoria3.GameData\GameColor.cs
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
            => $"#{R:X2}{G:X2}{B:X2}";

        public override string ToString()
            => ToColorCode();
    }
}
```

```Victoria3.GameData\HistoricalStateRegion.cs
namespace Victoria3.GameData
{
    /// <summary>
    /// 歴史的州地域を表すレコード。歴史的州地域は、ゲーム開始時点でどの国がどの州を所有しているかを表す。
    /// </summary>
    /// <param name="Tag">州地域のタグ</param>
    /// <param name="CreateStates">州地域に含まれる州のリスト</param>
    /// <param name="Homelands">この州地域を母国とする文化のリスト</param>
    /// <param name="Claims">この州地域に請求権を持つ国のリスト</param>
    public sealed record HistoricalStateRegion(
        string Tag,
        IReadOnlyList<CreateState> CreateStates,
        IReadOnlyList<string> Homelands,
        IReadOnlyList<string> Claims)
        : IPropertySchemaProvider<HistoricalStateRegion>
    {
        private static readonly PropertySchema<HistoricalStateRegion>[] _propertySchemas =
        [
            new PropertySchema<HistoricalStateRegion>(typeof(string), "Tag", c => c.Tag),
            new PropertySchema<HistoricalStateRegion>(typeof(IReadOnlyList<CreateState>), "Create States", c => c.CreateStates),
            new PropertySchema<HistoricalStateRegion>(typeof(IReadOnlyList<string>), "Homelands", c => c.Homelands),
            new PropertySchema<HistoricalStateRegion>(typeof(IReadOnlyList<string>), "Claims", c => c.Claims),
        ];

        /// <inheritdoc/>
        public static PropertySchema<HistoricalStateRegion>[] PropertySchemas
            => _propertySchemas;
    }
}
```

```Victoria3.GameData\IPropertySchemaProvider.cs
namespace Victoria3.GameData
{
    /// <summary>
    /// プロパティスキーマを提供するためのインターフェース。
    /// プロパティスキーマは、クラスのプロパティの型や名前、値へのアクセス方法を定義するもので、データの構造を動的に扱う際に使用される。
    /// </summary>
    /// <typeparam name="T">プロパティスキーマを提供するクラスの型</typeparam>
    public interface IPropertySchemaProvider<T>
    {
        /// <summary>
        /// クラスのプロパティスキーマの配列を取得する。
        /// </summary>
        public static abstract PropertySchema<T>[] PropertySchemas { get; }
    }
}
```

```Victoria3.GameData\PropertySchema.cs
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
```

```Victoria3.GameData\ReleasableCountry.cs
namespace Victoria3.GameData
{
    /// <summary>
    /// 解放可能国家を表すレコード。解放可能国家は、特定の条件を満たすことでゲーム内で解放されることができる国家を表す。
    /// </summary>
    /// <param name="Tag">国家のタグ。</param>
    /// <param name="States">必要な州のリスト。</param>
    /// <param name="Provinces">必要なプロヴィンスのリスト。</param>
    /// <param name="UseCultureStates">必要な州として文化に基づく州を使用するかどうか。</param>
    /// <param name="RequiredNumStates">必要な州の数。</param>
    /// <param name="AIWillDo">AIが実行するかどうか。</param>
    /// <param name="Possible">解放の発動条件。</param>
    public sealed record ReleasableCountry(
        string Tag,
        IReadOnlyList<string> States,
        IReadOnlyList<string> Provinces,
        bool UseCultureStates,
        int? RequiredNumStates,
        object? AIWillDo,
        object? Possible)
        : IPropertySchemaProvider<ReleasableCountry>
    {
        private static readonly PropertySchema<ReleasableCountry>[] _propertySchemas =
        [
            new PropertySchema<ReleasableCountry>(typeof(string), "Tag", c => c.Tag),
            new PropertySchema<ReleasableCountry>(typeof(IReadOnlyList<string>), "States", c => c.States),
            new PropertySchema<ReleasableCountry>(typeof(IReadOnlyList<string>), "Provinces", c => c.Provinces),
            new PropertySchema<ReleasableCountry>(typeof(bool), "Use Culture States", c => c.UseCultureStates),
            new PropertySchema<ReleasableCountry>(typeof(int), "Required States Num", c => c.RequiredNumStates),
            new PropertySchema<ReleasableCountry>(typeof(object), "AI Will Do", c => c.AIWillDo),
            new PropertySchema<ReleasableCountry>(typeof(object), "Possible", c => c.Possible),
        ];

        /// <inheritdoc/>
        public static PropertySchema<ReleasableCountry>[] PropertySchemas
            => _propertySchemas;
    }
}
```

```Victoria3.Loading\ColorConverter.cs
using Victoria3.GameData;

namespace Victoria3.Loading
{
    /// <summary>
    /// RGB および HSV 形式の色成分を <see cref="GameColor"/> に変換するユーティリティクラス。
    /// </summary>
    internal static class ColorConverter
    {
        /// <summary>
        /// 指定された RGB 値を使用して <see cref="GameColor"/> を作成する。
        /// </summary>
        /// <param name="r">赤成分 (0-255)</param>
        /// <param name="g">緑成分 (0-255)</param>
        /// <param name="b">青成分 (0-255)</param>
        /// <returns>作成された <see cref="GameColor"/> オブジェクト</returns>
        internal static GameColor FromRgb(byte r, byte g, byte b)
            => new(r, g, b);

        /// <summary>
        /// 指定された RGB 値を使用して <see cref="GameColor"/> を作成する。RGB 値は 0-255 の範囲であると仮定される。
        /// </summary>
        /// <param name="r">赤成分 (0-255)</param>
        /// <param name="g">緑成分 (0-255)</param>
        /// <param name="b">青成分 (0-255)</param>
        /// <returns>作成された <see cref="GameColor"/> オブジェクト</returns>
        internal static GameColor FromRgb(decimal r, decimal g, decimal b)
            => FromRgb((byte)r, (byte)g, (byte)b);

        /// <summary>
        /// 指定された HSV 値を使用して <see cref="GameColor"/> を作成する。HSV 値はそれぞれ 0-1 の範囲であると仮定される。
        /// </summary>
        /// <param name="h">色相 (0-1)</param>
        /// <param name="s">彩度 (0-1)</param>
        /// <param name="v">明度 (0-1)</param>
        /// <returns>作成された <see cref="GameColor"/> オブジェクト</returns>
        internal static GameColor FromHsv(float h, float s, float v)
        {
            int i = (int)(h * 6);
            float f = h * 6 - i;
            byte p = (byte)(v * 255 * (1 - s));
            byte q = (byte)(v * 255 * (1 - f * s));
            byte t = (byte)(v * 255 * (1 - (1 - f) * s));
            byte vByte = (byte)(v * 255);
            return i switch
            {
                0 => new GameColor { R = vByte, G = t, B = p },
                1 => new GameColor { R = q, G = vByte, B = p },
                2 => new GameColor { R = p, G = vByte, B = t },
                3 => new GameColor { R = p, G = q, B = vByte },
                4 => new GameColor { R = t, G = p, B = vByte },
                _ => new GameColor { R = vByte, G = p, B = q },
            };
        }

        /// <summary>
        /// 指定された HSV 値を使用して <see cref="GameColor"/> を作成する。HSV 値はそれぞれ 0-1 の範囲であると仮定される。
        /// </summary>
        /// <param name="h">色相 (0-1)</param>
        /// <param name="s">彩度 (0-1)</param>
        /// <param name="v">明度 (0-1)</param>
        /// <returns>作成された <see cref="GameColor"/> オブジェクト</returns>
        internal static GameColor FromHsv(decimal h, decimal s, decimal v)
            => FromHsv((float)h, (float)s, (float)v);

        /// <summary>
        /// 指定された HSV 値を使用して <see cref="GameColor"/> を作成する。HSV 値はそれぞれ 0-360 (色相)、0-100 (彩度)、0-100 (明度) の範囲であると仮定される。
        /// </summary>
        /// <param name="h">色相 (0-360)</param>
        /// <param name="s">彩度 (0-100)</param>
        /// <param name="v">明度 (0-100)</param>
        /// <returns>作成された <see cref="GameColor"/> オブジェクト</returns>
        internal static GameColor FromHsv360(decimal h, decimal s, decimal v)
            => FromHsv((float)h / 360f, (float)s / 100f, (float)v / 100f);
    }
}
```

```Victoria3.Loading\LoadOutput.cs
using PdxScriptAnalysis.Diagnostics;

namespace Victoria3.Loading
{
    /// <summary>
    /// ロードの出力を表すレコード。値と診断情報を含む。
    /// </summary>
    /// <typeparam name="T">ロードされるゲームデータの型。</typeparam>
    /// <param name="Values">ロードされたゲームデータのリスト。</param>
    /// <param name="Diagnostics">ロード中に発生した診断情報のリスト。</param>
    public sealed record LoadOutput<T>(
        IReadOnlyList<T> Values,
        IReadOnlyList<Diagnostic> Diagnostics);
}
```

```Victoria3.Loading\Victoria3Paths.cs
namespace Victoria3.Loading
{
    /// <summary>
    /// Victoria 3のゲームデータのパスを定義するクラス。
    /// </summary>
    public static class Victoria3Paths
    {
        public static string CountryDefinitions => @"common\country_definitions";
        public static string CountryFormation => @"common\country_formation";
        public static string CountryCreation => @"common\country_creation";
        public static string HistoricalStates => @"common\history\states";
    }
}
```

```Victoria3.Loading\Loaders\CountryLoader.cs
using PdxScriptAnalysis;
using PdxScriptAnalysis.Diagnostics;
using Victoria3.GameData;

namespace Victoria3.Loading.Loaders
{
    /// <summary>
    /// 国家データを <see cref="ScriptTree"/> から読み込むローダー。
    /// </summary>
    /// <param name="trees">読み込むスクリプトツリーのコレクション。</param>
    public sealed class CountryLoader(IEnumerable<ScriptTree> trees) : ILoader<Country>
    {
        private readonly IEnumerable<ScriptTree> _trees = trees;

        ///  <inheritdoc/>
        public LoadOutput<Country> Load()
        {
            var countries = new List<Country>();
            var diagnostics = new List<Diagnostic>();

            foreach (var tree in _trees)
            {
                var output = new CountryTreeLoader(tree).Load();
                countries.AddRange(output.Values);
                diagnostics.AddRange(output.Diagnostics);
            }

            return new(countries, diagnostics);
        }
    }
}
```

```Victoria3.Loading\Loaders\CountryTreeLoader.cs
using PdxScriptAnalysis;
using PdxScriptAnalysis.Diagnostics;
using PdxScriptAnalysis.Syntax;
using PdxScriptAnalysis.Text;
using System.Diagnostics.CodeAnalysis;
using Victoria3.GameData;

namespace Victoria3.Loading.Loaders
{
    /// <summary>
    /// 1つの<see cref="ScriptTree"/>から国家データを読み込むための内部クラス。
    /// </summary>
    /// <param name="tree">読み込むスクリプトツリー。</param>
    internal sealed class CountryTreeLoader(ScriptTree tree)
    {
        private readonly ScriptTree _tree = tree;
        private readonly List<Diagnostic> _diagnostics = [];

        private string? FilePath => _tree.Source.FilePath;


        /// <summary>
        /// スクリプトツリーから国家データを読み込み、診断情報とともに返す。
        /// 診断情報には、ファイルパス情報を含める。
        /// </summary>
        /// <returns>ロード結果</returns>
        internal LoadOutput<Country> Load()
        {
            _diagnostics.Clear();
            var countries = new List<Country>();

            foreach (var topLevelNode in _tree.Root.Children)
            {
                if (topLevelNode is not BlockPropertyNode blockNode)
                {
                    AddError($"Unexpected top-level node of type {topLevelNode.GetType().Name}. Expected a BlockPropertyNode representing a country definition.", topLevelNode.Span, topLevelNode.LinePosition);
                    continue;
                }

                if (TryLoadCountry(blockNode, out var country))
                {
                    countries.Add(country);
                }
            }

            return new(countries, _diagnostics);
        }

        // ブロックプロパティノードから国家データを読み込む。必須プロパティが不足している場合はエラー診断を追加し、false を返す。
        private bool TryLoadCountry(BlockPropertyNode node, [NotNullWhen(true)] out Country country)
        {
            var countryBuilder = new CountryBuilder();

            var tag = node.Key.Text;
            countryBuilder.Tag = tag;

            foreach (var child in node.Value.Children)
            {
                if (child is not PropertyNode propertyNode)
                {
                    AddError($"Unexpected child node of type {child.GetType().Name}. Expected a PropertyNode.", child.Span, child.LinePosition);
                    continue;
                }

                switch (propertyNode.Key.Text)
                {
                    case "color":
                        if (TryParseToGameColor(propertyNode, out var color)) countryBuilder.Color = color;
                        break;
                    case "country_type":
                        if (TryParseToEnum<CountryType>(propertyNode, out var type)) countryBuilder.Type = type;
                        break;
                    case "tier":
                        if (TryParseToEnum<CountryTier>(propertyNode, out var tier)) countryBuilder.Tier = tier;
                        break;
                    case "social_hierarchy":
                        if (TryParseToString(propertyNode, out var socialHierarchy)) countryBuilder.SocialHierarchy = socialHierarchy;
                        break;
                    case "religion":
                        if (TryParseToString(propertyNode, out var religion)) countryBuilder.Religion = religion;
                        break;
                    case "cultures":
                        if (TryParseToStringList(propertyNode, out var cultures)) countryBuilder.Cultures = cultures;
                        break;
                    case "capital":
                        if (TryParseToString(propertyNode, out var capital)) countryBuilder.Capital = capital;
                        break;
                    case "is_named_from_capital":
                        if (TryParseToBool(propertyNode, out var isNamedFromCapital)) countryBuilder.IsNamedFromCapital = isNamedFromCapital;
                        break;
                    case "valid_as_home_country_for_separatists":
                        // 一旦ノードをそのまま
                        countryBuilder.ValidAsHomeCountryForSeparatists = propertyNode;
                        break;
                    case "primary_unit_color":
                        if (TryParseToGameColor(propertyNode, out var primaryUnitColor)) countryBuilder.PrimaryUnitColor = primaryUnitColor;
                        break;
                    case "secondary_unit_color":
                        if (TryParseToGameColor(propertyNode, out var secondaryUnitColor)) countryBuilder.SecondaryUnitColor = secondaryUnitColor;
                        break;
                    case "tertiary_unit_color":
                        if (TryParseToGameColor(propertyNode, out var tertiaryUnitColor)) countryBuilder.TertiaryUnitColor = tertiaryUnitColor;
                        break;
                    case "dynamic_country_definition":
                        // dynamic_country_definition = yes のプロパティを持つ場合その国家は読み取らない
                        if (TryParseToBool(propertyNode, out var isDynamicCountryDefinition) && isDynamicCountryDefinition == true)
                        {
                            country = default!;
                            return false;
                        }
                        break;
                    default:
                        // 予期しないプロパティがあった場合は警告を追加する。
                        // バージョンアップなどで新しいプロパティが追加された場合に、古いバージョンのツールでも読み込みを続行できるようにするため。
                        AddWarning($"Unexpected property \"{propertyNode.Key.Text}\" in country definition. This property will be ignored.", propertyNode.Key.Span, propertyNode.LinePosition);
                        break;
                }
            }

            // 必須プロパティが不足している場合はエラー診断を追加し、false を返す。
            var missings = countryBuilder.GetMissingRequiredProperties();
            if (missings.Count > 0)
            {
                AddError($"Missing required properties ({string.Join(", ", missings)}) for country with tag \"{tag}\": {string.Join(", ", missings)}.", node.Span, node.LinePosition);
                country = default!;
                return false;
            }

            country = countryBuilder.Build();
            return true;
        }

        // PropertyNode から値をパースするためのヘルパーメソッド群
        // PropertyNodeParsers クラスの TryParse メソッドを呼び出し、失敗した場合は診断情報にファイルパスを追加する。
        private bool TryParseToString(PropertyNode node, [NotNullWhen(true)] out string value)
        {
            if (PropertyNodeParsers.TryParseToString(node, out value, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic with { FilePath = FilePath });
                value = null!;
                return false;
            }
        }

        private bool TryParseToStringList(PropertyNode node, [NotNullWhen(true)] out List<string> values)
        {
            if (PropertyNodeParsers.TryParseToStringList(node, out values, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic with { FilePath = FilePath });
                values = null!;
                return false;
            }
        }

        private bool TryParseToBool(PropertyNode node, out bool value)
        {
            if (PropertyNodeParsers.TryParseToBool(node, out value, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic with { FilePath = FilePath });
                value = false;
                return false;
            }
        }

        private bool TryParseToEnum<TEnum>(PropertyNode node, out TEnum value)
            where TEnum : struct, Enum
        {
            if (PropertyNodeParsers.TryParseToEnum(node, out value, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic with { FilePath = FilePath });
                value = default;
                return false;
            }
        }

        private bool TryParseToGameColor(PropertyNode node, out GameColor color)
        {
            if (PropertyNodeParsers.TryParseToGameColor(node, out color, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic with { FilePath = FilePath });
                color = default;
                return false;
            }
        }

        // エラー診断を追加するためのヘルパーメソッド
        private void AddError(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, message, span, linePosition, FilePath));
        private void AddWarning(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, message, span, linePosition, FilePath));

        // ビルダー。必須プロパティを null 許容型で保持し、ビルド時に不足しているプロパティをチェックする。
        private class CountryBuilder
        {
            internal string? Tag { get; set; }
            internal GameColor? Color { get; set; }
            internal CountryType? Type { get; set; }
            internal CountryTier? Tier { get; set; }
            internal string? SocialHierarchy { get; set; }
            internal string? Religion { get; set; }
            internal List<string> Cultures { get; set; } = [];
            internal string? Capital { get; set; }
            internal bool? IsNamedFromCapital { get; set; }
            internal object? ValidAsHomeCountryForSeparatists { get; set; }
            internal GameColor? PrimaryUnitColor { get; set; }
            internal GameColor? SecondaryUnitColor { get; set; }
            internal GameColor? TertiaryUnitColor { get; set; }


            internal Country Build()
                => new(
                    Tag: Tag!,
                    Color: Color!.Value,
                    Type: Type!.Value,
                    Tier: Tier!.Value,
                    SocialHierarchy: SocialHierarchy,
                    Religion: Religion,
                    Cultures: Cultures,
                    Capital: Capital,
                    IsNamedFromCapital: IsNamedFromCapital ?? false,
                    ValidAsHomeCountryForSeparatists: ValidAsHomeCountryForSeparatists,
                    PrimaryUnitColor: PrimaryUnitColor,
                    SecondaryUnitColor: SecondaryUnitColor,
                    TertiaryUnitColor: TertiaryUnitColor);

            internal List<string> GetMissingRequiredProperties()
            {
                var missingProperties = new List<string>();
                if (Tag is null) missingProperties.Add("Tag");
                if (Color is null) missingProperties.Add("Color");
                if (Type is null) missingProperties.Add("Type");
                if (Tier is null) missingProperties.Add("Tier");
                if (Cultures.Count == 0) missingProperties.Add("Cultures");
                return missingProperties;
            }
        }
    }
}
```

```Victoria3.Loading\Loaders\FormableCountryLoader.cs
using PdxScriptAnalysis;
using PdxScriptAnalysis.Diagnostics;
using PdxScriptAnalysis.Syntax;
using PdxScriptAnalysis.Text;
using System.Diagnostics.CodeAnalysis;
using Victoria3.GameData;

namespace Victoria3.Loading.Loaders
{
    /// <summary>
    /// 形成可能国家のロード処理を担当するクラス。
    /// </summary>
    /// <param name="trees">読み込むスクリプトツリーのコレクション。</param>
    public sealed class FormableCountryLoader(IEnumerable<ScriptTree> trees) : ILoader<FormableCountry>
    {
        private readonly IEnumerable<ScriptTree> _trees = trees;
        private readonly List<Diagnostic> _diagnostics = [];

        /// <inheritdoc/>
        public LoadOutput<FormableCountry> Load()
        {
            _diagnostics.Clear();
            var formables = new List<FormableCountry>();

            foreach (var tree in _trees)
            {
                var formablesFromTree = LoadFromTree(tree);
                formables.AddRange(formablesFromTree);
            }

            return new LoadOutput<FormableCountry>(formables, _diagnostics);
        }

        private List<FormableCountry> LoadFromTree(ScriptTree tree)
        {
            var formables = new List<FormableCountry>();

            foreach (var topLevelNode in tree.Root.Children)
            {
                if (topLevelNode is not BlockPropertyNode blockNode)
                {
                    AddError($"Unexpected top-level node of type {topLevelNode.GetType().Name}. Expected a BlockPropertyNode representing a formable country definition.", topLevelNode.Span, topLevelNode.LinePosition);
                    continue;
                }

                if (TryLoadFormableCountry(blockNode, out var formableCountry))
                {
                    formables.Add(formableCountry);
                }
            }

            return formables;
        }

        private bool TryLoadFormableCountry(BlockPropertyNode node, [NotNullWhen(true)] out FormableCountry formableCountry)
        {
            var formableCountryBuilder = new FormableCountryBuilder();

            var tag = node.Key.Text;
            formableCountryBuilder.Tag = tag;

            foreach (var child in node.Value.Children)
            {
                if (child is not PropertyNode propertyNode)
                {
                    AddError($"Unexpected child node of type {child.GetType().Name}. Expected a PropertyNode.", child.Span, child.LinePosition);
                    continue;
                }

                switch (propertyNode.Key.Text)
                {
                    case "states":
                    case "STATES":
                        if (TryParseToStringList(propertyNode, out var states)) formableCountryBuilder.States = states;
                        break;
                    case "use_culture_states":
                        if (TryParseToBool(propertyNode, out var useCultureStates)) formableCountryBuilder.UseCultureStates = useCultureStates;
                        break;
                    case "required_states_fraction":
                        if (TryParseToDecimal(propertyNode, out var requiredStatesFraction)) formableCountryBuilder.RequiredStatesFraction = requiredStatesFraction;
                        break;
                    case "ai_will_do":
                        formableCountryBuilder.AIWillDo = propertyNode;
                        break;
                    case "potential":
                        formableCountryBuilder.Potential = propertyNode;
                        break;
                    case "possible":
                        formableCountryBuilder.Possible = propertyNode;
                        break;
                    case "geographic_region":
                        if (TryParseToString(propertyNode, out var geographicRegion)) formableCountryBuilder.GeographicRegion = geographicRegion;
                        break;
                    case "is_major_formation":
                        if (TryParseToBool(propertyNode, out var isMajorFormation)) formableCountryBuilder.IsMajorFormation = isMajorFormation;
                        break;
                    case "unification_play":
                        if (TryParseToString(propertyNode, out var unificationPlay)) formableCountryBuilder.UnificationPlay = unificationPlay;
                        break;
                    case "leadership_play":
                        if (TryParseToString(propertyNode, out var leadershipPlay)) formableCountryBuilder.LeadershipPlay = leadershipPlay;
                        break;
                    case "max_num_formation_candidates":
                        if (TryParseToInt(propertyNode, out var maxNumFormationCandidates)) formableCountryBuilder.MaxNumFormationCandidates = maxNumFormationCandidates;
                        break;
                    case "can_be_formation_candidate":
                        formableCountryBuilder.CanBeFormationCandidate = propertyNode;
                        break;
                    case "can_be_unification_target":
                        formableCountryBuilder.CanBeUnificationTarget = propertyNode;
                        break;
                    default:
                        AddWarning($"Unexpected property \"{propertyNode.Key.Text}\" in country definition. This property will be ignored.", propertyNode.Key.Span, propertyNode.LinePosition);
                        break;
                }
            }

            var missings = formableCountryBuilder.GetMissingRequiredProperties();
            if (missings.Count > 0)
            {
                AddError($"Missing required properties for formable country with tag \"{tag}\": {string.Join(", ", missings)}.", node.Span, node.LinePosition);
                formableCountry = default!;
                return false;
            }

            formableCountry = formableCountryBuilder.Build();
            return true;
        }


        private bool TryParseToString(PropertyNode node, [NotNullWhen(true)] out string value)
        {
            if (PropertyNodeParsers.TryParseToString(node, out value, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic);
                value = null!;
                return false;
            }
        }

        private bool TryParseToStringList(PropertyNode node, [NotNullWhen(true)] out List<string> values)
        {
            if (PropertyNodeParsers.TryParseToStringList(node, out values, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic);
                values = null!;
                return false;
            }
        }

        private bool TryParseToBool(PropertyNode node, out bool value)
        {
            if (PropertyNodeParsers.TryParseToBool(node, out value, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic);
                value = false;
                return false;
            }
        }

        private bool TryParseToDecimal(PropertyNode node, out decimal value)
        {
            if (PropertyNodeParsers.TryParseToDecimal(node, out value, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic);
                value = 0;
                return false;
            }
        }

        private bool TryParseToInt(PropertyNode node, out int value)
        {
            if (PropertyNodeParsers.TryParseToInt(node, out value, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic);
                value = 0;
                return false;
            }
        }

        // エラー診断を追加するためのヘルパーメソッド
        private void AddError(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, message, span, linePosition));
        private void AddWarning(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, message, span, linePosition));

        // ビルダー。必須プロパティを null 許容型で保持し、ビルド時に不足しているプロパティをチェックする。
        private class FormableCountryBuilder
        {
            internal string? Tag { get; set; }
            internal List<string> States { get; set; } = [];
            internal bool? UseCultureStates { get; set; }
            internal decimal? RequiredStatesFraction { get; set; }
            internal object? AIWillDo { get; set; }
            internal object? Potential { get; set; }
            internal object? Possible { get; set; }
            internal string? GeographicRegion { get; set; }
            internal bool? IsMajorFormation { get; set; }
            internal string? UnificationPlay { get; set; }
            internal string? LeadershipPlay { get; set; }
            internal int? MaxNumFormationCandidates { get; set; }
            internal object? CanBeFormationCandidate { get; set; }
            internal object? CanBeUnificationTarget { get; set; }

            internal FormableCountry Build()
                => new(
                    Tag: Tag!,
                    States: States,
                    UseCultureStates: UseCultureStates ?? false,
                    RequiredStatesFraction: RequiredStatesFraction ?? 1,
                    AIWillDo: AIWillDo,
                    Potential: Potential,
                    Possible: Possible,
                    GeographicRegion: GeographicRegion,
                    IsMajorFormation: IsMajorFormation ?? false,
                    UnificationPlay: UnificationPlay,
                    LeadershipPlay: LeadershipPlay,
                    MaxNumFormationCandidates: MaxNumFormationCandidates,
                    CanBeFormationCandidate: CanBeFormationCandidate,
                    CanBeUnificationTarget: CanBeUnificationTarget
                    );

            internal List<string> GetMissingRequiredProperties()
            {
                var missingProperties = new List<string>();
                if (Tag is null) missingProperties.Add("Tag");
                if (States.Count == 0 && UseCultureStates != true && GeographicRegion is null) missingProperties.Add("States or UseCultureStates");
                if (IsMajorFormation == true)
                {
                    if (UnificationPlay is null) missingProperties.Add("UnificationPlay (required when IsMajorFormation is true)");
                    if (LeadershipPlay is null) missingProperties.Add("LeadershipPlay (required when IsMajorFormation is true)");
                    if (MaxNumFormationCandidates is null) missingProperties.Add("MaxNumFormationCandidates (required when IsMajorFormation is true)");
                    if (CanBeFormationCandidate is null) missingProperties.Add("CanBeFormationCandidate (required when IsMajorFormation is true)");
                }
                return missingProperties;
            }
        }
    }
}
```

```Victoria3.Loading\Loaders\HistoricalStateRegionLoader.cs
using PdxScriptAnalysis;
using PdxScriptAnalysis.Diagnostics;
using PdxScriptAnalysis.Syntax;
using PdxScriptAnalysis.Text;
using System.Diagnostics.CodeAnalysis;
using Victoria3.GameData;

namespace Victoria3.Loading.Loaders
{
    /// <summary>
    /// 歴史的州地域のデータをロードするクラス。
    /// </summary>
    /// <param name="trees">読み込むスクリプトツリーのコレクション。</param>
    public class HistoricalStateRegionLoader(IEnumerable<ScriptTree> trees) : ILoader<HistoricalStateRegion>
    {
        private readonly IEnumerable<ScriptTree> _trees = trees;
        private readonly List<Diagnostic> _diagnostics = [];

        /// <inheritdoc/>
        public LoadOutput<HistoricalStateRegion> Load()
        {
            _diagnostics.Clear();
            var historicalStateRegions = new List<HistoricalStateRegion>();

            foreach (var tree in _trees)
            {
                var historicalStateRegionsFromTree = LoadFromTree(tree);
                historicalStateRegions.AddRange(historicalStateRegionsFromTree);
            }

            return new LoadOutput<HistoricalStateRegion>(historicalStateRegions, _diagnostics);
        }

        private List<HistoricalStateRegion> LoadFromTree(ScriptTree tree)
        {
            var historicalStateRegions = new List<HistoricalStateRegion>();

            if (tree.Root.Children.Count != 1)
            {
                AddError($"Expected exactly one top-level node in the script tree, but found {tree.Root.Children.Count}.", tree.Root.Span, tree.Root.LinePosition);
                return historicalStateRegions;
            }
            var topLevelNode = tree.Root.Children[0];
            if (topLevelNode is not BlockPropertyNode blockPropertyNode)
            {
                AddError($"Unexpected top-level node of type {topLevelNode.GetType().Name}. Expected a BlockPropertyNode representing the root of the historical state region definition.", topLevelNode.Span, topLevelNode.LinePosition);
                return historicalStateRegions;
            }
            if (blockPropertyNode.Key.Text != "STATES")
            {
                AddError($"Unexpected top-level block with key \"{blockPropertyNode.Key.Text}\". Expected a block with the key \"STATES\" representing the root of the historical state region definition.", blockPropertyNode.Key.Span, blockPropertyNode.Key.LinePosition);
                return historicalStateRegions;
            }

            foreach (var node in blockPropertyNode.Value.Children)
            {
                if (node is not BlockPropertyNode blockNode)
                {
                    AddError($"Unexpected child node of type {node.GetType().Name} under the top-level STATES block. Expected a BlockPropertyNode representing a historical state region definition.", node.Span, node.LinePosition);
                    continue;
                }

                if (TryLoadHistoricalStateRegion(blockNode, out var historicalStateRegion))
                {
                    historicalStateRegions.Add(historicalStateRegion);
                }
            }

            return historicalStateRegions;
        }

        private bool TryLoadHistoricalStateRegion(BlockPropertyNode node, [NotNullWhen(true)] out HistoricalStateRegion historicalStateRegion)
        {
            var historicalStateRegionBuilder = new HistoricalStateRegionBuilder();

            var tag = node.Key.Text;
            historicalStateRegionBuilder.Tag = tag;

            foreach (var child in node.Value.Children)
            {
                if (child is not PropertyNode propertyNode)
                {
                    AddError($"Unexpected child node of type {child.GetType().Name}. Expected a PropertyNode.", child.Span, child.LinePosition);
                    continue;
                }

                switch (propertyNode.Key.Text)
                {
                    case "create_state":
                        if (TryParseToCreateState(propertyNode, out var createState)) historicalStateRegionBuilder.CreateStates.Add(createState);
                        break;
                    case "add_homeland":
                        if (TryParseToString(propertyNode, out var homeland)) historicalStateRegionBuilder.Homelands.Add(homeland);
                        break;
                    case "add_claim":
                        if (TryParseToString(propertyNode, out var claim)) historicalStateRegionBuilder.Claims.Add(claim);
                        break;
                    default:
                        AddWarning($"Unexpected property \"{propertyNode.Key.Text}\" in country definition. This property will be ignored.", propertyNode.Key.Span, propertyNode.LinePosition);
                        break;
                }
            }

            var missings = historicalStateRegionBuilder.GetMissingRequiredProperties();
            if (missings.Count > 0)
            {
                AddError($"Missing required properties for formable country with tag \"{tag}\": {string.Join(", ", missings)}.", node.Span, node.LinePosition);
                historicalStateRegion = default!;
                return false;
            }

            historicalStateRegion = historicalStateRegionBuilder.Build();
            return true;
        }

        private bool TryParseToString(PropertyNode node, [NotNullWhen(true)] out string value)
        {
            if (PropertyNodeParsers.TryParseToString(node, out value, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic);
                value = null!;
                return false;
            }
        }

        private bool TryParseToCreateState(PropertyNode node, [NotNullWhen(true)] out CreateState value)
        {
            if (node is not BlockPropertyNode createStateBlockNode)
            {
                AddError($"Expected a block property node for \"{node.Key.Text}\", but found a different type of node.", node.Span, node.LinePosition);
                value = null!;
                return false;
            }
            var createStateNodes = createStateBlockNode.Value.Children;
            if (!(createStateNodes.Count == 2 || createStateNodes.Count == 3))
            {
                AddError($"Expected exactly 2 or 3 child nodes under the \"{node.Key.Text}\" block for state creation definition, but found {createStateBlockNode.Value.Children.Count}.", createStateBlockNode.Span, createStateBlockNode.LinePosition);
                value = null!;
                return false;
            }
            if (!(createStateNodes.Any(n => n is ScalarPropertyNode scalar && scalar.Key.Text == "country") && createStateNodes.Any(n => n is BlockPropertyNode block && block.Key.Text == "owned_provinces")))
            {
                AddError($"Expected exactly one scalar property node with key \"country\" and one block property node with key \"owned_provinces\" under the \"create_state\" block for state creation definition, but the expected nodes were not found.", createStateBlockNode.Span, createStateBlockNode.LinePosition);
                value = null!;
                return false;
            }
            var country = createStateNodes
                .OfType<ScalarPropertyNode>()
                .FirstOrDefault(n => n.Key.Text == "country")?
                .Value.Token.Text;
            if (country is null)
            {
                AddError($"Expected a scalar property node with key \"country\" under the \"create_state\" block for state creation definition, but it was not found.", createStateBlockNode.Span, createStateBlockNode.LinePosition);
                value = null!;
                return false;
            }
            var provincesNode = createStateNodes
                .OfType<BlockPropertyNode>()
                .FirstOrDefault(n => n.Key.Text == "owned_provinces");
            if (provincesNode is null)
            {
                AddError($"Expected a block property node with key \"owned_provinces\" under the \"create_state\" block for state creation definition, but it was not found.", createStateBlockNode.Span, createStateBlockNode.LinePosition);
                value = null!;
                return false;
            }
            if (provincesNode.Value.Children.Any(c => c is not ScalarNode))
            {
                AddError($"Expected all child nodes under the \"owned_provinces\" block to be scalar nodes representing province IDs, but found child nodes of different types.", provincesNode.Span, provincesNode.LinePosition);
                value = null!;
                return false;
            }
            if (createStateNodes.Count == 3 && !createStateNodes.Any(n => n is ScalarPropertyNode scalar && scalar.Key.Text == "state_type"))
            {
                AddError($"Expected a scalar property node with key \"state_type\" as the optional third child node under the \"create_state\" block for state creation definition, but it was not found.", createStateBlockNode.Span, createStateBlockNode.LinePosition);
                value = null!;
                return false;
            }
            var stateType = createStateNodes
                .OfType<ScalarPropertyNode>()
                .FirstOrDefault(n => n.Key.Text == "state_type")?
                .Value.Token.Text;
            var provinces = provincesNode
                .Value.Children
                .OfType<ScalarNode>()
                .Select(n => n.Token.Text)
                .ToList();

            value = new CreateState(country, stateType, provinces);
            return true;
        }

        // エラー診断を追加するためのヘルパーメソッド
        private void AddError(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, message, span, linePosition));

        private void AddWarning(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, message, span, linePosition));

        // 歴史的州地域のビルダークラス。必須プロパティを null 許容型で保持し、ビルド時に不足しているプロパティをチェックする。
        private class HistoricalStateRegionBuilder
        {
            internal string? Tag { get; set; }
            internal List<CreateState> CreateStates { get; set; } = [];
            internal List<string> Homelands { get; set; } = [];
            internal List<string> Claims { get; set; } = [];

            internal HistoricalStateRegion Build()
                => new(
                    Tag: Tag!,
                    CreateStates: CreateStates,
                    Homelands: Homelands,
                    Claims: Claims
                    );

            internal List<string> GetMissingRequiredProperties()
            {
                var missingProperties = new List<string>();
                if (Tag is null) missingProperties.Add("Tag");
                return missingProperties;
            }
        }
    }
}
```

```Victoria3.Loading\Loaders\ILoader.cs
namespace Victoria3.Loading.Loaders
{
    /// <summary>
    /// ロード処理を表すインターフェース。
    /// </summary>
    /// <typeparam name="T">ロードされるデータの型。</typeparam>
    public interface ILoader<T>
    {
        /// <summary>
        /// ゲームデータをロードするメソッド。
        /// </summary>
        /// <returns>読み込まれたデータと診断情報を含む <see cref="LoadOutput{T}"/> オブジェクト</returns>
        public LoadOutput<T> Load();
    }
}
```

```Victoria3.Loading\Loaders\PropertyNodeParsers.cs
using PdxScriptAnalysis.Diagnostics;
using PdxScriptAnalysis.Syntax;
using PdxScriptAnalysis.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Victoria3.GameData;

namespace Victoria3.Loading.Loaders
{
    /// <summary>
    /// 汎用的なプロパティノードのパーサーを提供する静的クラス。プロパティノードを特定の型(文字列、ブール値、数値など)に変換するためのメソッドを含む。
    /// </summary>
    internal static class PropertyNodeParsers
    {
        /// <summary>
        /// プロパティノードを文字列に変換しようとする。
        /// ノードがスカラーでない場合や、スカラーの値が文字列でない場合は、適切な診断情報を返す。
        /// </summary>
        /// <param name="node">変換対象のプロパティノード。</param>
        /// <param name="value">変換結果の文字列。</param>
        /// <param name="diagnostic">変換に失敗した場合の診断情報。</param>
        /// <returns>変換が成功した場合はtrue、失敗した場合はfalse。</returns>
        internal static bool TryParseToString(
            PropertyNode node,
            [NotNullWhen(true)] out string value,
            [NotNullWhen(false)] out Diagnostic diagnostic)
        {
            if (node is not ScalarPropertyNode scalar)
            {
                value = null!;
                diagnostic = CreateError($"Expected a scalar property node for property \"{node.Key.Text}\", but found a different type of node.", node.Span, node.LinePosition);
                return false;
            }

            value = scalar.Value.Token.Text;
            diagnostic = null!;
            return true;
        }

        /// <summary>
        /// プロパティノードを文字列のリストに変換しようとする。
        /// ノードがブロックでない場合や、ブロックの子ノードがすべてスカラーでない場合は、適切な診断情報を返す。
        /// </summary>
        /// <param name="node">変換対象のプロパティノード。</param>
        /// <param name="values">変換結果の文字列リスト。</param>
        /// <param name="diagnostic">変換に失敗した場合の診断情報。</param>
        /// <returns>変換が成功した場合はtrue、失敗した場合はfalse。</returns>
        internal static bool TryParseToStringList(
            PropertyNode node,
            [NotNullWhen(true)] out List<string> values,
            [NotNullWhen(false)] out Diagnostic diagnostic)
        {
            if (node is not BlockPropertyNode block)
            {
                values = null!;
                diagnostic = CreateError($"Expected a block property node for property \"{node.Key.Text}\", but found a different type of node.", node.Span, node.LinePosition);
                return false;
            }

            if (block.Value.Children.Any(c => c is not ScalarNode))
            {
                values = null!;
                diagnostic = CreateError($"Expected all children of the block for property \"{node.Key.Text}\" to be scalar nodes representing string values, but found child nodes of different types.", block.Span, block.LinePosition);
                return false;
            }

            values = block.Value.Children
                .OfType<ScalarNode>()
                .Select(n => n.Token.Text)
                .ToList();
            diagnostic = null!;
            return true;
        }

        /// <summary>
        /// プロパティノードを真偽値に変換しようとする。
        /// ノードがスカラーでない場合や、スカラーの値が "yes" または "no" でない場合は、適切な診断情報を返す。
        /// </summary>
        /// <param name="node">変換対象のプロパティノード。</param>
        /// <param name="value">変換結果の真偽値。</param>
        /// <param name="diagnostic">変換に失敗した場合の診断情報。</param>
        /// <returns>変換が成功した場合はtrue、失敗した場合はfalse。</returns>
        internal static bool TryParseToBool(
            PropertyNode node,
            out bool value,
            [NotNullWhen(false)] out Diagnostic diagnostic)
        {
            if (node is not ScalarPropertyNode scalar)
            {
                value = default;
                diagnostic = CreateError($"Expected a scalar property node for property \"{node.Key.Text}\", but found a different type of node.", node.Span, node.LinePosition);
                return false;
            }

            switch (scalar.Value.Token.Text)
            {
                case "yes":
                    value = true;
                    diagnostic = null!;
                    return true;
                case "no":
                    value = false;
                    diagnostic = null!;
                    return true;
                default:
                    value = default;
                    diagnostic = CreateError($"Expected the value of property \"{node.Key.Text}\" to be \"yes\" or \"no\", but found \"{scalar.Value.Token.Text}\".", scalar.Value.Span, scalar.Value.LinePosition);
                    return false;
            }
        }

        /// <summary>
        /// プロパティノードを整数に変換しようとする。
        /// ノードがスカラーでない場合や、スカラーの値が有効な整数でない場合は、適切な診断情報を返す。
        /// </summary>
        /// <param name="node">変換対象のプロパティノード。</param>
        /// <param name="value">変換結果の整数値。</param>
        /// <param name="diagnostic">変換に失敗した場合の診断情報。</param>
        /// <returns>変換が成功した場合はtrue、失敗した場合はfalse。</returns>
        internal static bool TryParseToInt(
            PropertyNode node,
            out int value,
            [NotNullWhen(false)] out Diagnostic diagnostic)
        {
            if (node is not ScalarPropertyNode scalar)
            {
                value = default;
                diagnostic = CreateError($"Expected a scalar property node for property \"{node.Key.Text}\", but found a different type of node.", node.Span, node.LinePosition);
                return false;
            }

            if (!int.TryParse(scalar.Value.Token.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                value = default;
                diagnostic = CreateError($"Expected the value of property \"{node.Key.Text}\" to be a valid integer number, but found \"{scalar.Value.Token.Text}\".", scalar.Value.Span, scalar.Value.LinePosition);
                return false;
            }

            diagnostic = null!;
            return true;
        }

        /// <summary>
        /// プロパティノードを十進数に変換しようとする。
        /// ノードがスカラーでない場合や、スカラーの値が有効な十進数でない場合は、適切な診断情報を返す。
        /// </summary>
        /// <param name="node">変換対象のプロパティノード。</param>
        /// <param name="value">変換結果の十進数値。</param>
        /// <param name="diagnostic">変換に失敗した場合の診断情報。</param>
        /// <returns>変換が成功した場合はtrue、失敗した場合はfalse。</returns>
        internal static bool TryParseToDecimal(
            PropertyNode node,
            out decimal value,
            [NotNullWhen(false)] out Diagnostic diagnostic)
        {
            if (node is not ScalarPropertyNode scalar)
            {
                value = default;
                diagnostic = CreateError($"Expected a scalar property node for property \"{node.Key.Text}\", but found a different type of node.", node.Span, node.LinePosition);
                return false;
            }

            if (!decimal.TryParse(scalar.Value.Token.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            {
                value = default;
                diagnostic = CreateError($"Expected the value of property \"{node.Key.Text}\" to be a valid decimal number, but found \"{scalar.Value.Token.Text}\".", scalar.Value.Span, scalar.Value.LinePosition);
                return false;
            }

            diagnostic = null!;
            return true;
        }

        /// <summary>
        /// プロパティノードを列挙型の値に変換しようとする。
        /// </summary>
        /// <typeparam name="TEnum">変換先の列挙型。</typeparam>
        /// <param name="node">変換対象のプロパティノード。</param>
        /// <param name="value">変換結果の列挙型の値。</param>
        /// <param name="diagnostic">変換に失敗した場合の診断情報。</param>
        /// <returns>変換が成功した場合はtrue、失敗した場合はfalse。</returns>
        internal static bool TryParseToEnum<TEnum>(
            PropertyNode node,
            out TEnum value,
            [NotNullWhen(false)] out Diagnostic diagnostic)
            where TEnum : struct, Enum
        {
            if (!TryParseToString(node, out var raw, out diagnostic))
            {
                value = default;
                return false;
            }

            var normalizedRaw = raw
                .Replace("_", "", StringComparison.OrdinalIgnoreCase)
                .Replace("-", "", StringComparison.OrdinalIgnoreCase);

            if (!Enum.TryParse(normalizedRaw, ignoreCase: true, out value))
            {
                value = default;
                diagnostic = CreateError($"Invalid value \"{raw}\" for property \"{node.Key.Text}\". Expected one of: {string.Join(", ", Enum.GetNames<TEnum>())}.", node.Span, node.LinePosition);
                return false;
            }

            diagnostic = null!;
            return true;
        }

        /// <summary>
        /// プロパティノードをゲーム内の色を表すGameColor構造体に変換しようとする。
        /// </summary>
        /// <param name="node">変換対象のプロパティノード。</param>
        /// <param name="color">変換結果のGameColor構造体。</param>
        /// <param name="diagnostic">変換に失敗した場合の診断情報。</param>
        /// <returns>変換が成功した場合はtrue、失敗した場合はfalse。</returns>
        internal static bool TryParseToGameColor(
            PropertyNode node,
            out GameColor color,
            [NotNullWhen(false)] out Diagnostic diagnostic)
        {
            switch (node)
            {
                case BlockPropertyNode block:
                    if (!TryParseFromBlockToColorValues(block.Value, node.Key.Text, out var colorValues, out diagnostic))
                    {
                        color = default;
                        return false;
                    }

                    color = ColorConverter.FromRgb(colorValues[0], colorValues[1], colorValues[2]);
                    diagnostic = null!;
                    return true;
                case TypedBlockPropertyNode typedBlock:
                    if (!TryParseFromBlockToColorValues(typedBlock.Value, node.Key.Text, out var typedColorValues, out diagnostic))
                    {
                        color = default;
                        return false;
                    }

                    var typeQualifier = typedBlock.TypeQualifier.Text;
                    if (typeQualifier.Equals("hsv", StringComparison.OrdinalIgnoreCase))
                    {
                        color = ColorConverter.FromHsv(typedColorValues[0], typedColorValues[1], typedColorValues[2]);
                        diagnostic = null!;
                        return true;
                    }
                    else if (typeQualifier.Equals("hsv360", StringComparison.OrdinalIgnoreCase))
                    {
                        color = ColorConverter.FromHsv360(typedColorValues[0], typedColorValues[1], typedColorValues[2]);
                        diagnostic = null!;
                        return true;
                    }
                    else if (typeQualifier.Equals("rgb", StringComparison.OrdinalIgnoreCase))
                    {
                        color = ColorConverter.FromRgb(typedColorValues[0], typedColorValues[1], typedColorValues[2]);
                        diagnostic = null!;
                        return true;
                    }
                    else
                    {
                        color = default;
                        diagnostic = CreateError($"Invalid type qualifier \"{typeQualifier}\" for typed block property \"{node.Key.Text}\". Expected one of: \"rgb\", \"hsv\", \"hsv360\".", typedBlock.TypeQualifier.Span, typedBlock.TypeQualifier.LinePosition);
                        return false;
                    }
                default:
                    color = default;
                    diagnostic = CreateError($"Expected a block or typed block property node for property \"{node.Key.Text}\", but found a different type of node.", node.Span, node.LinePosition);
                    return false;
            }
        }

        // ブロックノードの子ノードを色の値として解析するためのヘルパーメソッド
        private static bool TryParseFromBlockToColorValues(BlockNode block, string propertyName, out decimal[] colorValues, [NotNullWhen(false)] out Diagnostic diagnostic)
        {
            if (block.Children.Count != 3)
            {
                colorValues = [];
                diagnostic = CreateError($"Expected a block with exactly 3 children for property \"{propertyName}\" to represent RGB values, but found a block with {block.Children.Count} children.", block.Span, block.LinePosition);
                return false;
            }

            if (block.Children.Any(c => c is not ScalarNode))
            {
                colorValues = [];
                diagnostic = CreateError($"Expected all children of the block for property \"{propertyName}\" to be scalar nodes representing numeric color values, but found child nodes of different types.", block.Span, block.LinePosition);
                return false;
            }

            var colorValueNodes = block.Children.OfType<ScalarNode>().ToList();

            colorValues = new decimal[3];
            for (int i = 0; i < 3; i++)
            {
                if (!decimal.TryParse(colorValueNodes[i].Token.Text, out colorValues[i]))
                {
                    colorValues = [];
                    diagnostic = CreateError($"Expected the value of child node {i + 1} of the block for property \"{propertyName}\" to be a valid decimal number representing a color component, but found \"{colorValueNodes[i].Token.Text}\".", colorValueNodes[i].Span, colorValueNodes[i].LinePosition);
                    return false;
                }
            }
            diagnostic = null!;
            return true;
        }


        // エラー診断を作成するためのヘルパーメソッド。エラーメッセージ、テキストスパン、および行位置を受け取り、Diagnosticオブジェクトを返す。
        private static Diagnostic CreateError(string message, TextSpan span, LinePosition linePosition)
            => new(DiagnosticSeverity.Error, message, span, linePosition);
    }
}
```

```Victoria3.Loading\Loaders\ReleasableCountryLoader.cs
using PdxScriptAnalysis;
using PdxScriptAnalysis.Diagnostics;
using PdxScriptAnalysis.Syntax;
using PdxScriptAnalysis.Text;
using System.Diagnostics.CodeAnalysis;
using Victoria3.GameData;

namespace Victoria3.Loading.Loaders
{
    /// <summary>
    /// 解放可能国家のデータをロードするクラス。
    /// </summary>
    /// <param name="trees">読み込むスクリプトツリーのコレクション。</param>
    public class ReleasableCountryLoader(IEnumerable<ScriptTree> trees) : ILoader<ReleasableCountry>
    {
        private readonly IEnumerable<ScriptTree> _trees = trees;
        private readonly List<Diagnostic> _diagnostics = [];

        /// <inheritdoc/>
        public LoadOutput<ReleasableCountry> Load()
        {
            _diagnostics.Clear();
            var releasables = new List<ReleasableCountry>();

            foreach (var tree in _trees)
            {
                var releasablesFromTree = LoadFromTree(tree);
                releasables.AddRange(releasablesFromTree);
            }

            return new LoadOutput<ReleasableCountry>(releasables, _diagnostics);
        }

        private List<ReleasableCountry> LoadFromTree(ScriptTree tree)
        {
            var releasables = new List<ReleasableCountry>();

            foreach (var topLevelNode in tree.Root.Children)
            {
                if (topLevelNode is not BlockPropertyNode blockNode)
                {
                    AddError($"Unexpected top-level node of type {topLevelNode.GetType().Name}. Expected a BlockPropertyNode representing a releasable country definition.", topLevelNode.Span, topLevelNode.LinePosition);
                    continue;
                }

                if (TryLoadReleasableCountry(blockNode, out var releasableCountry))
                {
                    releasables.Add(releasableCountry);
                }
            }

            return releasables;
        }

        private bool TryLoadReleasableCountry(BlockPropertyNode node, [NotNullWhen(true)] out ReleasableCountry releasableCountry)
        {
            var releasableCountryBuilder = new ReleasableCountryBuilder();

            var tag = node.Key.Text;
            releasableCountryBuilder.Tag = tag;

            foreach (var child in node.Value.Children)
            {
                if (child is not PropertyNode propertyNode)
                {
                    AddError($"Unexpected child node of type {child.GetType().Name}. Expected a PropertyNode.", child.Span, child.LinePosition);
                    continue;
                }

                switch (propertyNode.Key.Text)
                {
                    case "states":
                    case "STATES":
                        if (TryParseToStringList(propertyNode, out var states)) releasableCountryBuilder.States = states;
                        break;
                    case "provinces":
                        if (TryParseToStringList(propertyNode, out var provinces)) releasableCountryBuilder.Provinces = provinces;
                        break;
                    case "use_culture_states":
                        if (TryParseToBool(propertyNode, out var useCultureStates)) releasableCountryBuilder.UseCultureStates = useCultureStates;
                        break;
                    case "required_num_states":
                        if (TryParseToInt(propertyNode, out var requiredNumStates)) releasableCountryBuilder.RequiredNumStates = requiredNumStates;
                        break;
                    case "ai_will_do":
                        releasableCountryBuilder.AIWillDo = propertyNode;
                        break;
                    case "possible":
                        releasableCountryBuilder.Possible = propertyNode;
                        break;
                    default:
                        AddWarning($"Unexpected property \"{propertyNode.Key.Text}\" in country definition. This property will be ignored.", propertyNode.Key.Span, propertyNode.LinePosition);
                        break;
                }
            }

            var missings = releasableCountryBuilder.GetMissingRequiredProperties();
            if (missings.Count > 0)
            {
                AddError($"Missing required properties for formable country with tag \"{tag}\": {string.Join(", ", missings)}.", node.Span, node.LinePosition);
                releasableCountry = default!;
                return false;
            }

            releasableCountry = releasableCountryBuilder.Build();
            return true;
        }


        private bool TryParseToStringList(PropertyNode node, [NotNullWhen(true)] out List<string> values)
        {
            if (PropertyNodeParsers.TryParseToStringList(node, out values, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic);
                values = null!;
                return false;
            }
        }

        private bool TryParseToBool(PropertyNode node, out bool value)
        {
            if (PropertyNodeParsers.TryParseToBool(node, out value, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic);
                value = false;
                return false;
            }
        }

        private bool TryParseToInt(PropertyNode node, out int value)
        {
            if (PropertyNodeParsers.TryParseToInt(node, out value, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic);
                value = 0;
                return false;
            }
        }

        // エラー診断を追加するためのヘルパーメソッド
        private void AddError(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, message, span, linePosition));

        private void AddWarning(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, message, span, linePosition));

        // ビルダー。必須プロパティを null 許容型で保持し、ビルド時に不足しているプロパティをチェックする。
        private class ReleasableCountryBuilder
        {
            internal string? Tag { get; set; }
            internal List<string> States { get; set; } = [];
            internal List<string> Provinces { get; set; } = [];
            internal bool? UseCultureStates { get; set; }
            internal int? RequiredNumStates { get; set; }
            internal object? AIWillDo { get; set; }
            internal object? Possible { get; set; }

            internal ReleasableCountry Build()
                => new(
                    Tag: Tag!,
                    States: States,
                    Provinces: Provinces,
                    UseCultureStates: UseCultureStates ?? false,
                    RequiredNumStates: RequiredNumStates,
                    AIWillDo: AIWillDo,
                    Possible: Possible
                    );

            internal List<string> GetMissingRequiredProperties()
            {
                var missingProperties = new List<string>();
                if (Tag is null) missingProperties.Add("Tag");
                if (States.Count == 0 && Provinces.Count == 0 && UseCultureStates != true) missingProperties.Add("States or Provinces or UseCultureStates");
                return missingProperties;
            }
        }
    }
}
```

```Victoria3.Loading.Tests\Loaders\CountryLoaderTests.cs
using FluentAssertions;
using PdxScriptAnalysis;
using Victoria3.GameData;
using Victoria3.Loading.Loaders;

namespace Victoria3.Loading.Tests.Loaders
{
    public class CountryLoaderTests
    {
        private const string MinimalCountry = """
        GER = {
            color = { 147 130 110 }
            country_type = recognized
            tier = empire
            cultures = { north_german }
            capital = STATE_BRANDENBURG
        }
        """;

        private static IEnumerable<ScriptTree> ParseTrees(params string[] texts)
            => texts.Select(ScriptTree.ParseText);

        // --- 正常系 ---

        [Fact(DisplayName = "最小構成のデータを読み込むと必須フィールドが正しく読み込まれる")]
        public void Load_MinimalCountry_ParsesRequiredFields()
        {
            var loader = new CountryLoader(ParseTrees(MinimalCountry));
            var output = loader.Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Should().ContainSingle();

            var country = output.Values[0];
            country.Tag.Should().Be("GER");
            country.Color.Should().Be(new GameColor(147, 130, 110));
            country.Type.Should().Be(CountryType.Recognized);
            country.Tier.Should().Be(CountryTier.Empire);
            country.Cultures.Should().Equal("north_german");
            country.Capital.Should().Be("STATE_BRANDENBURG");
        }

        [Fact(DisplayName = "最小構成のデータを読み込むとオプションフィールドはデフォルト値になる")]
        public void Load_MinimalCountry_OptionalFieldsHaveDefaultValues()
        {
            var loader = new CountryLoader(ParseTrees(MinimalCountry));
            var output = loader.Load();

            var country = output.Values[0];
            country.SocialHierarchy.Should().BeNull();
            country.Religion.Should().BeNull();
            country.IsNamedFromCapital.Should().BeFalse();
            country.ValidAsHomeCountryForSeparatists.Should().BeNull();
            country.PrimaryUnitColor.Should().BeNull();
            country.SecondaryUnitColor.Should().BeNull();
            country.TertiaryUnitColor.Should().BeNull();
        }

        [Fact(DisplayName = "すべてのオプションフィールドをロードできる")]
        public void Load_AllOptionalFields_CanBeLoaded()
        {
            var input = """
            JPN = {
                color = { 255 0 0 }
                country_type = recognized
                tier = empire
                social_hierarchy = monarchy
                religion = shinto
                cultures = { japanese }
                capital = STATE_KANTO
                is_named_from_capital = yes
                valid_as_home_country_for_separatists = { foo = bar }
                primary_unit_color = rgb { 10 20 30 }
                secondary_unit_color = hsv { 0.0 0.0 1.0 }
                tertiary_unit_color = hsv360 { 0 0 100 }
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Should().ContainSingle();

            var country = output.Values[0];
            country.SocialHierarchy.Should().Be("monarchy");
            country.Religion.Should().Be("shinto");
            country.IsNamedFromCapital.Should().BeTrue();
            country.ValidAsHomeCountryForSeparatists.Should().NotBeNull();
            country.PrimaryUnitColor.Should().Be(new GameColor(10, 20, 30));
            country.SecondaryUnitColor.Should().Be(new GameColor(255, 255, 255));
            country.TertiaryUnitColor.Should().Be(new GameColor(255, 255, 255));
        }

        [Fact(DisplayName = "1つのスクリプトツリー上の複数データをロードできる")]
        public void Load_MultipleCountriesInSingleTree_CanBeLoaded()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            FRA = {
                color = { 50 100 200 }
                country_type = recognized
                tier = kingdom
                cultures = { french }
                capital = STATE_ILE_DE_FRANCE
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Select(x => x.Tag).Should().Equal("GER", "FRA");
        }

        [Fact(DisplayName = "複数のスクリプトツリーのデータをロードできる")]
        public void Load_MultipleTrees_CanBeLoaded()
        {
            var tree1 = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """;
            var tree2 = """
            FRA = {
                color = { 50 100 200 }
                country_type = recognized
                tier = kingdom
                cultures = { french }
                capital = STATE_ILE_DE_FRANCE
            }
            """;
            var loader = new CountryLoader(ParseTrees(tree1, tree2));
            var output = loader.Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Select(x => x.Tag).Should().Equal("GER", "FRA");
        }

        [Theory(DisplayName = "CountryType のパース（Country固有データ）が正しく動作する")]
        [InlineData("recognized", CountryType.Recognized)]
        [InlineData("colonial", CountryType.Colonial)]
        [InlineData("unrecognized", CountryType.Unrecognized)]
        [InlineData("decentralized", CountryType.Decentralized)]
        public void Load_CountryTypeParsing_Works(string rawType, CountryType expected)
        {
            var input = $$"""
            X = {
                color = { 1 2 3 }
                country_type = {{rawType}}
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;
            var output = new CountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values[0].Type.Should().Be(expected);
        }

        [Theory(DisplayName = "CountryTier のパース（Country固有データ）が正しく動作する")]
        [InlineData("hegemony", CountryTier.Hegemony)]
        [InlineData("empire", CountryTier.Empire)]
        [InlineData("grand-principality", CountryTier.GrandPrincipality)]
        public void Load_CountryTierParsing_Works(string rawTier, CountryTier expected)
        {
            var input = $$"""
            X = {
                color = { 1 2 3 }
                country_type = recognized
                tier = {{rawTier}}
                cultures = { foo }
                capital = STATE_X
            }
            """;
            var output = new CountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values[0].Tier.Should().Be(expected);
        }

        [Fact(DisplayName = "Color のパース（Country固有データ）が正しく動作する")]
        public void Load_ColorParsing_Works()
        {
            var input = """
            X = {
                color = hsv360 { 0 0 100 }
                country_type = recognized
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;
            var output = new CountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values[0].Color.Should().Be(new GameColor(255, 255, 255));
        }

        [Fact(DisplayName = "Load() を再呼び出しすると診断がリセットされる")]
        public void Load_CalledTwice_DiagnosticsAreReset()
        {
            var invalid = """
            X = {
                country_type = recognized
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;
            var loader = new CountryLoader(ParseTrees(invalid));

            var first = loader.Load();
            var second = loader.Load();

            first.Diagnostics.Count(d => d.IsError).Should().Be(1);
            second.Diagnostics.Count(d => d.IsError).Should().Be(1); // 累積しない
        }

        // --- 異常系 ---

        [Theory(DisplayName = "各必須フィールドの欠損でエラーになる")]
        [InlineData("""
            GER = {
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """, "Color")]
        [InlineData("""
            GER = {
                color = { 147 130 110 }
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """, "Type")]
        [InlineData("""
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """, "Tier")]
        [InlineData("""
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                capital = STATE_BRANDENBURG
            }
            """, "Cultures")]
        public void Load_MissingRequiredField_ReturnsError(string input, string missingField)
        {
            var output = new CountryLoader(ParseTrees(input)).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError && d.Message.Contains(missingField));
        }

        [Fact(DisplayName = "トップレベルノードが無効ならエラー")]
        public void Load_InvalidTopLevelNode_ReturnsError()
        {
            var output = new CountryLoader(ParseTrees("some_scalar_value")).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
        }

        [Fact(DisplayName = "ロード可能データと不可能データが混在する場合、可能なデータはロードされエラーも返る")]
        public void Load_MixedValidAndInvalidEntries_LoadsValidAndReturnsError()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            BAD = {
                country_type = recognized
            }
            FRA = {
                color = { 50 100 200 }
                country_type = recognized
                tier = kingdom
                cultures = { french }
                capital = STATE_ILE_DE_FRANCE
            }
            """;
            var output = new CountryLoader(ParseTrees(input)).Load();

            output.Values.Select(x => x.Tag).Should().Equal("GER", "FRA");
            output.Diagnostics.Should().Contain(d => d.IsError);
        }

        [Fact(DisplayName = "不明なプロパティがある場合は警告になる")]
        public void Load_UnknownProperty_ReturnsWarning()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
                unknown_prop = foo
            }
            """;

            var output = new CountryLoader(ParseTrees(input)).Load();

            output.Values.Should().ContainSingle();
            output.Diagnostics.Should().ContainSingle(d => d.IsWarning && d.Message.Contains("unknown_prop"));
        }
    }
}
```

```Victoria3.Loading.Tests\Loaders\FormableCountryLoaderTests.cs
using FluentAssertions;
using PdxScriptAnalysis;
using Victoria3.Loading.Loaders;

namespace Victoria3.Loading.Tests.Loaders
{
    public class FormableCountryLoaderTests
    {
        private static IEnumerable<ScriptTree> ParseTrees(params string[] texts)
            => texts.Select(ScriptTree.ParseText);

        [Fact(DisplayName = "蠢・域擅莉ｶ譛ｪ貅(States縺ｪ縺励・UseCultureStates縺ｪ縺・縺ｧ縺ｯ繧ｨ繝ｩ繝ｼ")]
        public void Load_MinimalWithoutStatesOrUseCultureStates_ReturnsError()
        {
            var input = """
            GER = { }
            """;

            var output = new FormableCountryLoader(ParseTrees(input)).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError && d.Message.Contains("States or UseCultureStates"));
        }

        [Fact(DisplayName = "states 縺後≠繧後・繝ｭ繝ｼ繝峨〒縺阪ｋ")]
        public void Load_WithStates_CanBeLoaded()
        {
            var input = """
            GER = {
                states = { STATE_BRANDENBURG }
            }
            """;

            var output = new FormableCountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Should().ContainSingle();
            output.Values[0].Tag.Should().Be("GER");
            output.Values[0].States.Should().Equal("STATE_BRANDENBURG");
            output.Values[0].UseCultureStates.Should().BeFalse();
            output.Values[0].RequiredStatesFraction.Should().Be(1m);
        }

        [Fact(DisplayName = "use_culture_states = yes 縺ｧ states 縺ｪ縺励〒繧ゅΟ繝ｼ繝峨〒縺阪ｋ")]
        public void Load_WithUseCultureStatesYes_WithoutStates_CanBeLoaded()
        {
            var input = """
            GER = {
                use_culture_states = yes
            }
            """;

            var output = new FormableCountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Should().ContainSingle();
            output.Values[0].UseCultureStates.Should().BeTrue();
            output.Values[0].States.Should().BeEmpty();
        }

        [Fact(DisplayName = "is_major_formation = yes 縺ｧ譚｡莉ｶ莉倥″蠢・医′荳崎ｶｳ縺吶ｋ縺ｨ繧ｨ繝ｩ繝ｼ")]
        public void Load_MajorFormationMissingConditionalRequired_ReturnsError()
        {
            var input = """
            GER = {
                states = { STATE_BRANDENBURG }
                is_major_formation = yes
            }
            """;

            var output = new FormableCountryLoader(ParseTrees(input)).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("UnificationPlay"));
            output.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("LeadershipPlay"));
            output.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("MaxNumFormationCandidates"));
            output.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("CanBeFormationCandidate"));
        }

        [Fact(DisplayName = "is_major_formation = yes 縺ｧ譚｡莉ｶ莉倥″蠢・医ｒ貅縺溘○縺ｰ繝ｭ繝ｼ繝峨〒縺阪ｋ")]
        public void Load_MajorFormationWithAllRequired_CanBeLoaded()
        {
            var input = """
            GER = {
                states = { STATE_BRANDENBURG }
                is_major_formation = yes
                unification_play = german_unification
                leadership_play = german_leadership
                max_num_formation_candidates = 3
                can_be_formation_candidate = { always = yes }
            }
            """;

            var output = new FormableCountryLoader(ParseTrees(input)).Load();
            var value = output.Values[0];

            output.Diagnostics.Should().BeEmpty();
            output.Values.Should().ContainSingle();
            value.IsMajorFormation.Should().BeTrue();
            value.UnificationPlay.Should().Be("german_unification");
            value.LeadershipPlay.Should().Be("german_leadership");
            value.MaxNumFormationCandidates.Should().Be(3);
            value.CanBeFormationCandidate.Should().NotBeNull();
        }

        [Fact(DisplayName = "縺吶∋縺ｦ縺ｮ繧ｪ繝励す繝ｧ繝ｳ繝輔ぅ繝ｼ繝ｫ繝峨ｒ繝ｭ繝ｼ繝峨〒縺阪ｋ")]
        public void Load_AllOptionalFields_CanBeLoaded()
        {
            var input = """
            GER = {
                states = { STATE_BRANDENBURG STATE_SAXONY }
                use_culture_states = yes
                required_states_fraction = 0.5
                ai_will_do = { base = 1 }
                potential = { always = yes }
                possible = { always = yes }
                geographic_region = central_europe
                is_major_formation = yes
                unification_play = german_unification
                leadership_play = german_leadership
                max_num_formation_candidates = 3
                can_be_formation_candidate = { always = yes }
                can_be_unification_target = { always = yes }
            }
            """;

            var output = new FormableCountryLoader(ParseTrees(input)).Load();
            var f = output.Values[0];

            output.Diagnostics.Should().BeEmpty();
            f.Tag.Should().Be("GER");
            f.States.Should().Equal("STATE_BRANDENBURG", "STATE_SAXONY");
            f.UseCultureStates.Should().BeTrue();
            f.RequiredStatesFraction.Should().Be(0.5m);
            f.AIWillDo.Should().NotBeNull();
            f.Potential.Should().NotBeNull();
            f.Possible.Should().NotBeNull();
            f.GeographicRegion.Should().Be("central_europe");
            f.IsMajorFormation.Should().BeTrue();
            f.UnificationPlay.Should().Be("german_unification");
            f.LeadershipPlay.Should().Be("german_leadership");
            f.MaxNumFormationCandidates.Should().Be(3);
            f.CanBeFormationCandidate.Should().NotBeNull();
            f.CanBeUnificationTarget.Should().NotBeNull();
        }

        [Fact(DisplayName = "1縺､縺ｮ繧ｹ繧ｯ繝ｪ繝励ヨ繝・Μ繝ｼ荳翫・隍・焚繝・・繧ｿ繧偵Ο繝ｼ繝峨〒縺阪ｋ")]
        public void Load_MultipleInSingleTree_CanBeLoaded()
        {
            var input = """
            GER = {
                states = { STATE_BRANDENBURG }
            }
            FRA = {
                states = { STATE_ILE_DE_FRANCE }
            }
            """;

            var output = new FormableCountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Select(x => x.Tag).Should().Equal("GER", "FRA");
        }

        [Fact(DisplayName = "隍・焚縺ｮ繧ｹ繧ｯ繝ｪ繝励ヨ繝・Μ繝ｼ縺ｮ繝・・繧ｿ繧偵Ο繝ｼ繝峨〒縺阪ｋ")]
        public void Load_MultipleTrees_CanBeLoaded()
        {
            var t1 = """
                GER = {
                    states = { STATE_BRANDENBURG }
                }
                """;
            var t2 = """
                FRA = {
                    states = { STATE_ILE_DE_FRANCE }
                }
                """;

            var output = new FormableCountryLoader(ParseTrees(t1, t2)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Select(x => x.Tag).Should().Equal("GER", "FRA");
        }

        [Fact(DisplayName = "STATES 繧ｭ繝ｼ縺ｧ繧・states 縺ｨ縺励※繝ｭ繝ｼ繝峨〒縺阪ｋ")]
        public void Load_StatesUppercase_Works()
        {
            var input = """
            GER = {
                STATES = { STATE_BRANDENBURG }
            }
            """;

            var output = new FormableCountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values[0].States.Should().Equal("STATE_BRANDENBURG");
        }

        [Fact(DisplayName = "Load() 縺ｮ蜀榊他縺ｳ蜃ｺ縺励〒險ｺ譁ｭ縺後Μ繧ｻ繝・ヨ縺輔ｌ繧・)]
        public void Load_CalledTwice_DiagnosticsAreReset()
        {
            var invalid = """
            GER = {
                required_states_fraction = abc
            }
            """;

            var loader = new FormableCountryLoader(ParseTrees(invalid));

            var first = loader.Load();
            var second = loader.Load();

            first.Diagnostics.Count(d => d.IsError).Should().Be(2);
            second.Diagnostics.Count(d => d.IsError).Should().Be(2);
        }

        [Fact(DisplayName = "繝医ャ繝励Ξ繝吶Ν繝弱・繝峨′辟｡蜉ｹ縺ｪ繧峨お繝ｩ繝ｼ")]
        public void Load_InvalidTopLevelNode_ReturnsError()
        {
            var output = new FormableCountryLoader(ParseTrees("foo")).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
        }

        [Fact(DisplayName = "譛牙柑繝・・繧ｿ縺ｨ荳肴ｭ｣繝・・繧ｿ縺梧ｷｷ蝨ｨ縺励※繧ゅΟ繝ｼ繝峨・邯咏ｶ壹＠繧ｨ繝ｩ繝ｼ繧りｿ斐ｋ")]
        public void Load_MixedValidAndInvalidEntries_ReturnsErrorAndContinues()
        {
            var input = """
            GER = { required_states_fraction = abc }
            FRA = { states = { STATE_ILE_DE_FRANCE } }
            """;

            var output = new FormableCountryLoader(ParseTrees(input)).Load();

            output.Values.Should().HaveCount(1);
            output.Diagnostics.Should().HaveCount(2);
            output.Diagnostics.Should().Contain(d => d.IsError);
        }

        [Fact(DisplayName = "荳肴・縺ｪ繝励Ο繝代ユ繧｣縺後≠繧句ｴ蜷医・隴ｦ蜻翫↓縺ｪ繧・)]
        public void Load_UnknownProperty_ReturnsWarning()
        {
            var input = """
            GER = {
                foo = bar
            }
            """;

            var output = new FormableCountryLoader(ParseTrees(input)).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsWarning && d.Message.Contains("foo"));
        }
    }
}
```

```Victoria3.Loading.Tests\Loaders\HistoricalStateRegionLoaderTests.cs
using FluentAssertions;
using PdxScriptAnalysis;
using Victoria3.GameData;
using Victoria3.Loading.Loaders;

namespace Victoria3.Loading.Tests.Loaders
{
    public class HistoricalStateRegionLoaderTests
    {
        private static IEnumerable<ScriptTree> ParseTrees(params string[] texts)
            => texts.Select(ScriptTree.ParseText);

        [Fact(DisplayName = "譛蟆乗ｧ区・縺ｮ繝・・繧ｿ繧定ｪｭ縺ｿ霎ｼ繧√ｋ")]
        public void Load_MinimalHistoricalStateRegion_CanBeLoaded()
        {
            var input = """
            STATES = {
                STATE_BRANDENBURG = { }
            }
            """;

            var output = new HistoricalStateRegionLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Should().ContainSingle();
            output.Values[0].Tag.Should().Be("STATE_BRANDENBURG");
        }

        [Fact(DisplayName = "create_state 繧・隕∫ｴ蠖｢蠑上〒繝ｭ繝ｼ繝峨〒縺阪ｋ")]
        public void Load_CreateState_TwoChildren_Works()
        {
            var input = """
            STATES = {
                STATE_BRANDENBURG = {
                    create_state = {
                        country = GER
                        owned_provinces = { x1 x2 }
                    }
                }
            }
            """;

            var output = new HistoricalStateRegionLoader(ParseTrees(input)).Load();
            var createState = output.Values[0].CreateStates.Single();

            output.Diagnostics.Should().BeEmpty();
            createState.Country.Should().Be("GER");
            createState.StateType.Should().BeNull();
            createState.Provinces.Should().Equal("x1", "x2");
        }

        [Fact(DisplayName = "create_state 繧・隕∫ｴ蠖｢蠑・state_type莉倥″)縺ｧ繝ｭ繝ｼ繝峨〒縺阪ｋ")]
        public void Load_CreateState_WithStateType_Works()
        {
            var input = """
            STATES = {
                STATE_BRANDENBURG = {
                    create_state = {
                        country = GER
                        owned_provinces = { x1 x2 }
                        state_type = incorporated
                    }
                }
            }
            """;

            var output = new HistoricalStateRegionLoader(ParseTrees(input)).Load();
            var createState = output.Values[0].CreateStates.Single();

            output.Diagnostics.Should().BeEmpty();
            createState.Country.Should().Be("GER");
            createState.StateType.Should().Be("incorporated");
            createState.Provinces.Should().Equal("x1", "x2");
        }

        [Fact(DisplayName = "add_homeland 縺ｨ add_claim 繧定､・焚繝ｭ繝ｼ繝峨〒縺阪ｋ")]
        public void Load_HomelandsAndClaims_Works()
        {
            var input = """
            STATES = {
                STATE_BRANDENBURG = {
                    add_homeland = north_german
                    add_homeland = south_german
                    add_claim = GER
                    add_claim = PRU
                }
            }
            """;

            var output = new HistoricalStateRegionLoader(ParseTrees(input)).Load();
            var r = output.Values[0];

            output.Diagnostics.Should().BeEmpty();
            r.Homelands.Should().Equal("north_german", "south_german");
            r.Claims.Should().Equal("GER", "PRU");
        }

        [Fact(DisplayName = "1縺､縺ｮSTATES繝悶Ο繝・け蜀・・隍・焚繝・・繧ｿ繧偵Ο繝ｼ繝峨〒縺阪ｋ")]
        public void Load_MultipleRegionsInSingleTree_CanBeLoaded()
        {
            var input = """
            STATES = {
                STATE_A = { }
                STATE_B = { }
            }
            """;

            var output = new HistoricalStateRegionLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Select(x => x.Tag).Should().Equal("STATE_A", "STATE_B");
        }

        [Fact(DisplayName = "隍・焚繧ｹ繧ｯ繝ｪ繝励ヨ繝・Μ繝ｼ縺ｮ繝・・繧ｿ繧偵Ο繝ｼ繝峨〒縺阪ｋ")]
        public void Load_MultipleTrees_CanBeLoaded()
        {
            var t1 = """
            STATES = {
                STATE_A = { }
            }
            """;
            var t2 = """
            STATES = {
                STATE_B = { }
            }
            """;

            var output = new HistoricalStateRegionLoader(ParseTrees(t1, t2)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Select(x => x.Tag).Should().Equal("STATE_A", "STATE_B");
        }

        [Fact(DisplayName = "Load() 縺ｮ蜀榊他縺ｳ蜃ｺ縺励〒險ｺ譁ｭ縺後Μ繧ｻ繝・ヨ縺輔ｌ繧・)]
        public void Load_CalledTwice_DiagnosticsAreReset()
        {
            var invalid = """
            STATES = {
                STATE_A = {
                    create_state = {
                        country = GER
                    }
                }
            }
            """;

            var loader = new HistoricalStateRegionLoader(ParseTrees(invalid));

            var first = loader.Load();
            var second = loader.Load();

            first.Diagnostics.Count(d => d.IsError).Should().Be(1);
            second.Diagnostics.Count(d => d.IsError).Should().Be(1);
        }

        [Fact(DisplayName = "繝医ャ繝励Ξ繝吶Ν繝弱・繝画焚縺御ｸ肴ｭ｣縺ｪ繧峨お繝ｩ繝ｼ")]
        public void Load_TopLevelNodeCountInvalid_ReturnsError()
        {
            var input = """
            STATES = { }
            OTHER = { }
            """;

            var output = new HistoricalStateRegionLoader(ParseTrees(input)).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
        }

        [Fact(DisplayName = "繝医ャ繝励Ξ繝吶Ν繧ｭ繝ｼ縺郡TATES縺ｧ縺ｪ縺代ｌ縺ｰ繧ｨ繝ｩ繝ｼ")]
        public void Load_TopLevelKeyInvalid_ReturnsError()
        {
            var input = """
            FOO = { }
            """;

            var output = new HistoricalStateRegionLoader(ParseTrees(input)).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
        }

        [Fact(DisplayName = "荳肴・縺ｪ繝励Ο繝代ユ繧｣縺後≠繧句ｴ蜷医・隴ｦ蜻翫↓縺ｪ繧・)]
        public void Load_UnknownProperty_ReturnsWarning()
        {
            var input = """
            STATES = {
                STATE_A = {
                    foo = bar
                }
            }
            """;

            var output = new HistoricalStateRegionLoader(ParseTrees(input)).Load();

            output.Values.Should().ContainSingle();
            output.Diagnostics.Should().ContainSingle(d => d.IsWarning && d.Message.Contains("foo"));
        }
    }
}
```

```Victoria3.Loading.Tests\Loaders\PropertyNodeParsersTests.cs
using FluentAssertions;
using PdxScriptAnalysis;
using PdxScriptAnalysis.Syntax;
using Victoria3.GameData;
using Victoria3.Loading.Loaders;

namespace Victoria3.Loading.Tests.Loaders
{
    public class PropertyNodeParsersTests
    {
        private enum TestEnum
        {
            GrandPrincipality,
        }

        private static PropertyNode ParseSinglePropertyNode(string text)
        {
            var root = ScriptTree.ParseText(text).Root;
            root.Children.Should().ContainSingle();
            return root.Children[0].Should().BeAssignableTo<PropertyNode>().Subject;
        }

        [Fact(DisplayName = "TryParseToString: ScalarPropertyNode を文字列として解析できる")]
        public void TryParseToString_WithScalarProperty_ReturnsTrue()
        {
            var node = ParseSinglePropertyNode("name = test_country");

            var ok = PropertyNodeParsers.TryParseToString(node, out var value, out var diagnostic);

            ok.Should().BeTrue();
            value.Should().Be("test_country");
            diagnostic.Should().BeNull();
        }

        [Fact(DisplayName = "TryParseToString: BlockPropertyNode を渡すと失敗する")]
        public void TryParseToString_WithBlockProperty_ReturnsFalse()
        {
            var node = ParseSinglePropertyNode("name = { test_country }");

            var ok = PropertyNodeParsers.TryParseToString(node, out var value, out var diagnostic);

            ok.Should().BeFalse();
            value.Should().BeNull();
            diagnostic.Should().NotBeNull();
            diagnostic.Message.Should().Contain("Expected a scalar property node");
        }

        [Fact(DisplayName = "TryParseToStringList: 文字列リストを解析できる")]
        public void TryParseToStringList_WithScalarChildren_ReturnsTrue()
        {
            var node = ParseSinglePropertyNode("cultures = { north_german south_german }");

            var ok = PropertyNodeParsers.TryParseToStringList(node, out var values, out var diagnostic);

            ok.Should().BeTrue();
            values.Should().Equal("north_german", "south_german");
            diagnostic.Should().BeNull();
        }

        [Fact(DisplayName = "TryParseToStringList: 子要素にスカラー以外があると失敗する")]
        public void TryParseToStringList_WithNonScalarChild_ReturnsFalse()
        {
            var node = ParseSinglePropertyNode("cultures = { north_german { foo = bar } }");

            var ok = PropertyNodeParsers.TryParseToStringList(node, out var values, out var diagnostic);

            ok.Should().BeFalse();
            values.Should().BeNull();
            diagnostic.Should().NotBeNull();
            diagnostic.Message.Should().Contain("Expected all children of the block");
        }

        [Theory(DisplayName = "TryParseToBool: yes/no を真偽値に変換できる")]
        [InlineData("yes", true)]
        [InlineData("no", false)]
        public void TryParseToBool_ValidValue_ReturnsTrue(string raw, bool expected)
        {
            var node = ParseSinglePropertyNode($"flag = {raw}");

            var ok = PropertyNodeParsers.TryParseToBool(node, out var value, out var diagnostic);

            ok.Should().BeTrue();
            value.Should().Be(expected);
            diagnostic.Should().BeNull();
        }

        [Fact(DisplayName = "TryParseToBool: yes/no 以外は失敗する")]
        public void TryParseToBool_InvalidValue_ReturnsFalse()
        {
            var node = ParseSinglePropertyNode("flag = true");

            var ok = PropertyNodeParsers.TryParseToBool(node, out _, out var diagnostic);

            ok.Should().BeFalse();
            diagnostic.Should().NotBeNull();
            diagnostic.Message.Should().Contain("yes").And.Contain("no");
        }

        [Fact(DisplayName = "TryParseToInt: 整数を解析できる")]
        public void TryParseToInt_ValidValue_ReturnsTrue()
        {
            var node = ParseSinglePropertyNode("rank = 42");

            var ok = PropertyNodeParsers.TryParseToInt(node, out var value, out var diagnostic);

            ok.Should().BeTrue();
            value.Should().Be(42);
            diagnostic.Should().BeNull();
        }

        [Fact(DisplayName = "TryParseToDecimal: 小数を解析できる")]
        public void TryParseToDecimal_ValidValue_ReturnsTrue()
        {
            var node = ParseSinglePropertyNode("ratio = 12.5");

            var ok = PropertyNodeParsers.TryParseToDecimal(node, out var value, out var diagnostic);

            ok.Should().BeTrue();
            value.Should().Be(12.5m);
            diagnostic.Should().BeNull();
        }

        [Theory(DisplayName = "TryParseToEnum: '_' '-' を正規化して列挙値に変換できる")]
        [InlineData("grand_principality")]
        [InlineData("grand-principality")]
        public void TryParseToEnum_NormalizedText_ReturnsTrue(string raw)
        {
            var node = ParseSinglePropertyNode($"tier = {raw}");

            var ok = PropertyNodeParsers.TryParseToEnum<TestEnum>(node, out var value, out var diagnostic);

            ok.Should().BeTrue();
            value.Should().Be(TestEnum.GrandPrincipality);
            diagnostic.Should().BeNull();
        }

        [Fact(DisplayName = "TryParseToGameColor: RGB ブロックを解析できる")]
        public void TryParseToGameColor_RgbBlock_ReturnsTrue()
        {
            var node = ParseSinglePropertyNode("color = { 147 130 110 }");

            var ok = PropertyNodeParsers.TryParseToGameColor(node, out var color, out var diagnostic);

            ok.Should().BeTrue();
            color.Should().Be(new GameColor(147, 130, 110));
            diagnostic.Should().BeNull();
        }

        [Fact(DisplayName = "TryParseToGameColor: HSV 型ブロックを RGB に変換できる")]
        public void TryParseToGameColor_HsvTypedBlock_ReturnsTrue()
        {
            var node = ParseSinglePropertyNode("color = hsv { 0.0 0.0 1.0 }");

            var ok = PropertyNodeParsers.TryParseToGameColor(node, out var color, out var diagnostic);

            ok.Should().BeTrue();
            color.Should().Be(new GameColor(255, 255, 255));
            diagnostic.Should().BeNull();
        }

        [Fact(DisplayName = "TryParseToGameColor: 不正な type qualifier は失敗する")]
        public void TryParseToGameColor_InvalidTypeQualifier_ReturnsFalse()
        {
            var node = ParseSinglePropertyNode("color = cmyk { 0 0 0 }");

            var ok = PropertyNodeParsers.TryParseToGameColor(node, out _, out var diagnostic);

            ok.Should().BeFalse();
            diagnostic.Should().NotBeNull();
            diagnostic.Message.Should().Contain("Invalid type qualifier");
        }
    }
}
```

```Victoria3.Loading.Tests\Loaders\ReleasableCountryLoaderTests.cs
using FluentAssertions;
using PdxScriptAnalysis;
using Victoria3.Loading.Loaders;

namespace Victoria3.Loading.Tests.Loaders
{
    public class ReleasableCountryLoaderTests
    {
        private static IEnumerable<ScriptTree> ParseTrees(params string[] texts)
            => texts.Select(ScriptTree.ParseText);

        [Fact(DisplayName = "蠢・域擅莉ｶ譛ｪ貅(States/Provinces縺ｪ縺励・UseCultureStates縺ｪ縺・縺ｧ縺ｯ繧ｨ繝ｩ繝ｼ")]
        public void Load_MinimalWithoutStatesProvincesUseCultureStates_ReturnsError()
        {
            var input = """
            GER = { }
            """;

            var output = new ReleasableCountryLoader(ParseTrees(input)).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError && d.Message.Contains("States or Provinces or UseCultureStates"));
        }

        [Fact(DisplayName = "states 縺後≠繧後・繝ｭ繝ｼ繝峨〒縺阪ｋ")]
        public void Load_WithStates_CanBeLoaded()
        {
            var input = """
            GER = {
                states = { STATE_BRANDENBURG }
            }
            """;

            var output = new ReleasableCountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Should().ContainSingle();
            output.Values[0].States.Should().Equal("STATE_BRANDENBURG");
            output.Values[0].Provinces.Should().BeEmpty();
        }

        [Fact(DisplayName = "provinces 縺後≠繧後・繝ｭ繝ｼ繝峨〒縺阪ｋ")]
        public void Load_WithProvinces_CanBeLoaded()
        {
            var input = """
            GER = {
                provinces = { x12345 x67890 }
            }
            """;

            var output = new ReleasableCountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Should().ContainSingle();
            output.Values[0].Provinces.Should().Equal("x12345", "x67890");
            output.Values[0].States.Should().BeEmpty();
        }

        [Fact(DisplayName = "use_culture_states = yes 縺ｧ states/provinces 縺ｪ縺励〒繧ゅΟ繝ｼ繝峨〒縺阪ｋ")]
        public void Load_WithUseCultureStatesYes_WithoutStatesOrProvinces_CanBeLoaded()
        {
            var input = """
            GER = {
                use_culture_states = yes
            }
            """;

            var output = new ReleasableCountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Should().ContainSingle();
            output.Values[0].UseCultureStates.Should().BeTrue();
        }

        [Fact(DisplayName = "縺吶∋縺ｦ縺ｮ繧ｪ繝励す繝ｧ繝ｳ繝輔ぅ繝ｼ繝ｫ繝峨ｒ繝ｭ繝ｼ繝峨〒縺阪ｋ")]
        public void Load_AllOptionalFields_CanBeLoaded()
        {
            var input = """
            GER = {
                states = { STATE_BRANDENBURG STATE_SAXONY }
                provinces = { x12345 x67890 }
                use_culture_states = yes
                required_num_states = 2
                ai_will_do = { base = 1 }
                possible = { always = yes }
            }
            """;

            var output = new ReleasableCountryLoader(ParseTrees(input)).Load();
            var r = output.Values[0];

            output.Diagnostics.Should().BeEmpty();
            r.States.Should().Equal("STATE_BRANDENBURG", "STATE_SAXONY");
            r.Provinces.Should().Equal("x12345", "x67890");
            r.UseCultureStates.Should().BeTrue();
            r.RequiredNumStates.Should().Be(2);
            r.AIWillDo.Should().NotBeNull();
            r.Possible.Should().NotBeNull();
        }

        [Fact(DisplayName = "1縺､縺ｮ繧ｹ繧ｯ繝ｪ繝励ヨ繝・Μ繝ｼ荳翫・隍・焚繝・・繧ｿ繧偵Ο繝ｼ繝峨〒縺阪ｋ")]
        public void Load_MultipleInSingleTree_CanBeLoaded()
        {
            var input = """
                GER = {
                    states = { STATE_BRANDENBURG }
                }
                FRA = {
                    states = { STATE_ILE_DE_FRANCE }
                }
                """;

            var output = new ReleasableCountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Select(x => x.Tag).Should().Equal("GER", "FRA");
        }

        [Fact(DisplayName = "隍・焚縺ｮ繧ｹ繧ｯ繝ｪ繝励ヨ繝・Μ繝ｼ縺ｮ繝・・繧ｿ繧偵Ο繝ｼ繝峨〒縺阪ｋ")]
        public void Load_MultipleTrees_CanBeLoaded()
        {
            var t1 = """
                GER = {
                    states = { STATE_BRANDENBURG }
                }
                """;
            var t2 = """
                FRA = {
                    states = { STATE_ILE_DE_FRANCE }
                }
                """;

            var output = new FormableCountryLoader(ParseTrees(t1, t2)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values.Select(x => x.Tag).Should().Equal("GER", "FRA");
        }

        [Fact(DisplayName = "STATES 繧ｭ繝ｼ縺ｧ繧・states 縺ｨ縺励※繝ｭ繝ｼ繝峨〒縺阪ｋ")]
        public void Load_StatesUppercase_Works()
        {
            var input = """
            GER = {
                STATES = { STATE_BRANDENBURG }
            }
            """;

            var output = new ReleasableCountryLoader(ParseTrees(input)).Load();

            output.Diagnostics.Should().BeEmpty();
            output.Values[0].States.Should().Equal("STATE_BRANDENBURG");
        }

        [Fact(DisplayName = "Load() 縺ｮ蜀榊他縺ｳ蜃ｺ縺励〒險ｺ譁ｭ縺後Μ繧ｻ繝・ヨ縺輔ｌ繧・)]
        public void Load_CalledTwice_DiagnosticsAreReset()
        {
            var invalid = """
            GER = {
                required_num_states = abc
            }
            """;

            var loader = new ReleasableCountryLoader(ParseTrees(invalid));

            var first = loader.Load();
            var second = loader.Load();

            first.Diagnostics.Count(d => d.IsError).Should().Be(2);
            second.Diagnostics.Count(d => d.IsError).Should().Be(2);
        }

        [Fact(DisplayName = "繝医ャ繝励Ξ繝吶Ν繝弱・繝峨′辟｡蜉ｹ縺ｪ繧峨お繝ｩ繝ｼ")]
        public void Load_InvalidTopLevelNode_ReturnsError()
        {
            var output = new ReleasableCountryLoader(ParseTrees("foo")).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
        }

        [Fact(DisplayName = "譛牙柑繝・・繧ｿ縺ｨ荳肴ｭ｣繝・・繧ｿ縺梧ｷｷ蝨ｨ縺励※繧ゅΟ繝ｼ繝峨・邯咏ｶ壹＠繧ｨ繝ｩ繝ｼ繧りｿ斐ｋ")]
        public void Load_MixedValidAndInvalidEntries_ReturnsErrorAndContinues()
        {
            var input = """
            GER = { required_num_states = abc }
            FRA = { states = { STATE_ILE_DE_FRANCE } }
            """;

            var output = new ReleasableCountryLoader(ParseTrees(input)).Load();

            output.Values.Should().HaveCount(1);
            output.Diagnostics.Should().HaveCount(2);
            output.Diagnostics.Should().Contain(d => d.IsError);
        }

        [Fact(DisplayName = "荳肴・縺ｪ繝励Ο繝代ユ繧｣縺後≠繧句ｴ蜷医・隴ｦ蜻翫↓縺ｪ繧・)]
        public void Load_UnknownProperty_ReturnsWarning()
        {
            var input = """
            GER = {
                foo = bar
            }
            """;

            var output = new ReleasableCountryLoader(ParseTrees(input)).Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsWarning && d.Message.Contains("foo"));
        }
    }
}
```

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
        public string Localize(string? key, bool removePrefix = true)
        {
            if (key is null) return string.Empty;
            if (removePrefix)
            {
                key = RemovePrefix(key);
            }
            return _localizations.TryGetValue(key, out var value) ? value : key;
        }

        /// <inheritdoc/>
        public bool TryLocalize(string? key, [NotNullWhen(true)] out string value, bool removePrefix = true)
        {
            if (key is null)
            {
                value = null!;
                return false;
            }
            if (removePrefix)
            {
                key = RemovePrefix(key);
            }
            return _localizations.TryGetValue(key, out value!);
        }

        private static string RemovePrefix(string key)
        {
            var index = key.IndexOf(':');
            if (index >= 0)
            {
                return key[(index + 1)..];
            }
            return key;
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
        /// キーがnullの場合は空文字列を返す。
        /// </summary>
        /// <param name="key">変換するキー。</param>
        /// <param name="removePrefix">キーのプレフィックスを除去するかどうか。デフォルトはtrue。</param>
        /// <returns>変換された文字列。見つからない場合はキー自体を返す。</returns>
        public string Localize(string? key, bool removePrefix = true);

        /// <summary>
        /// 指定されたキーを対応する文字列に変換し、成功したかどうかを示す。
        /// キーがnullの場合はfalseを返し、valueにはnullが設定される。
        /// </summary>
        /// <param name="key">変換するキー。</param>
        /// <param name="value">変換された文字列。見つからない場合はnull。</param>
        /// <param name="removePrefix">キーのプレフィックスを除去するかどうか。デフォルトはtrue。</param>
        /// <returns>変換が成功した場合はtrue、失敗した場合はfalse。</returns>
        public bool TryLocalize(string? key, [NotNullWhen(true)] out string value, bool removePrefix = true);
    }
}
```

```Victoria3.Localization\LocalizationPaths.cs
namespace Victoria3.Localization
{
    /// <summary>
    /// 翻訳ファイルのパスを定義するクラス。
    /// </summary>
    public static class LocalizationPaths
    {
        public static string Japanese => @"localization\japanese";
        public static string English => @"localization\english";

        /// <summary>
        /// 言語名を指定して対応する翻訳ファイルのパスを取得するメソッド。
        /// </summary>
        /// <param name="language">取得したい翻訳ファイルの言語名。</param>
        /// <returns>指定された言語に対応する翻訳ファイルのパス。</returns>
        /// <exception cref="ArgumentException">サポートされていない言語名が指定された場合にスローされる。</exception>
        public static string GetPath(string language)
            => language.ToLower() switch
            {
                "japanese" => Japanese,
                "english" => English,
                _ => throw new ArgumentException($"Unsupported language: {language}", nameof(language))
            };
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
                // ただし、空行は TryParseLine が失敗するため、個別の処理は行わない
                // if (line.Trim().IsEmpty) continue;

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

```Victoria3.Localization.Tests\FileLocalizerFileSystemTests.cs
using FluentAssertions;

namespace Victoria3.Localization.Tests
{
    public class FileLocalizerFileSystemTests
    {
        [Fact(DisplayName = "FromPath は実ファイルを読み込んでローカライズできる")]
        public void FromPath_ReadsSingleFile()
        {
            var dir = CreateTempDirectory();
            try
            {
                var file = Path.Combine(dir, "single.yml");
                File.WriteAllText(file, """
                    key:0 "value"
                    """);

                var localizer = FileLocalizer.FromPath(file);

                localizer.Localize("key").Should().Be("value");
            }
            finally
            {
                SafeDeleteDirectory(dir);
            }
        }

        [Fact(DisplayName = "FromPaths は後勝ちでマージされる")]
        public void FromPaths_MergesWithLastWriteWins()
        {
            var dir = CreateTempDirectory();
            try
            {
                var file1 = Path.Combine(dir, "a.yml");
                var file2 = Path.Combine(dir, "b.yml");

                File.WriteAllText(file1, """
                    dup:0 "first"
                    """);
                File.WriteAllText(file2, """
                    dup:0 "second"
                    """);

                var localizer = FileLocalizer.FromPaths(new[] { file1, file2 });

                localizer.Localize("dup").Should().Be("second");
            }
            finally
            {
                SafeDeleteDirectory(dir);
            }
        }

        [Fact(DisplayName = "FromDirectory は *.yml のみ再帰的に読み込む")]
        public void FromDirectory_ReadsOnlyYmlRecursively()
        {
            var dir = CreateTempDirectory();
            try
            {
                var sub = Path.Combine(dir, "sub");
                Directory.CreateDirectory(sub);

                File.WriteAllText(Path.Combine(dir, "root.yml"), """
                    root:0 "root-value"
                    """);
                File.WriteAllText(Path.Combine(sub, "child.yml"), """
                    child:0 "child-value"
                    """);
                File.WriteAllText(Path.Combine(dir, "ignore.txt"), """
                    ignored:0 "ignored-value"
                    """);

                var localizer = FileLocalizer.FromDirectory(dir);

                localizer.Localize("root").Should().Be("root-value");
                localizer.Localize("child").Should().Be("child-value");
                localizer.Localize("ignored").Should().Be("ignored");
            }
            finally
            {
                SafeDeleteDirectory(dir);
            }
        }

        [Fact(DisplayName = "FromDirectory の並び順に基づき後勝ちで上書きされる")]
        public void FromDirectory_MergeOrder_IsApplied()
        {
            var dir = CreateTempDirectory();
            try
            {
                // 実装は OrderByDescending(f => f) なので z -> a の順に読み込まれ、a が最終的に勝つ
                File.WriteAllText(Path.Combine(dir, "a.yml"), """
                    dup:0 "A"
                    """);
                File.WriteAllText(Path.Combine(dir, "z.yml"), """
                    dup:0 "Z"
                    """);

                var localizer = FileLocalizer.FromDirectory(dir);

                localizer.Localize("dup").Should().Be("A");
            }
            finally
            {
                SafeDeleteDirectory(dir);
            }
        }

        private static string CreateTempDirectory()
        {
            var dir = Path.Combine(Path.GetTempPath(), "Victoria3.Localization.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static void SafeDeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
    }
}
```

```Victoria3.Localization.Tests\FileLocalizerTests.cs
using FluentAssertions;

namespace Victoria3.Localization.Tests
{
    public class FileLocalizerTests
    {
        [Fact(DisplayName = "Localize・TryLocalizeが想定通りの挙動を示す")]
        public void LocalizeAndTryLocalize_WorkAsExpected()
        {
            var localizer = FileLocalizer.FromText("""
                known:0 "known-value"
                """);

            localizer.Localize("known").Should().Be("known-value");
            localizer.Localize("unknown").Should().Be("unknown");

            localizer.TryLocalize("known", out var known).Should().BeTrue();
            known.Should().Be("known-value");

            localizer.TryLocalize("unknown", out var _).Should().BeFalse();
        }

        [Fact(DisplayName = "キーがnullの場合、Localizeは空文字列を返す")]
        public void Localize_NullKey_ReturnsEmptyString()
        {
            var localizer = FileLocalizer.FromText("""
                known:0 "known-value"
                """);

            localizer.Localize(null).Should().Be(string.Empty);
        }

        [Fact(DisplayName = "キーがnullの場合、TryLocalizeはfalseを返す")]
        public void TryLocalize_NullKey_ReturnsFalse()
        {
            var localizer = FileLocalizer.FromText("""
                known:0 "known-value"
                """);
            localizer.TryLocalize(null, out var _).Should().BeFalse();
        }

        [Fact(DisplayName = "キーに接頭辞がある場合、接頭辞を削除してローカライズされる")]
        public void Localize_KeyWithPrefix_PrefixRemoved()
        {
            var localizer = FileLocalizer.FromText("""
                known:0 "known-value"
                """);
            localizer.Localize("prefix:known").Should().Be("known-value");
            localizer.TryLocalize("prefix:known", out var value).Should().BeTrue();
            value.Should().Be("known-value");
        }
    }
}
```

```Victoria3.Localization.Tests\LocalizationParserTests.cs
using FluentAssertions;
using Victoria3.Localization.Parsing;

namespace Victoria3.Localization.Tests
{
    public class LocalizationParserTests
    {
        [Fact(DisplayName = "基本形 key: \"value\" から辞書が作成される")]
        public void ParseText_BasicKeyValue_CreatesDictionary()
        {
            var text = """
            greeting: "hello"
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("greeting").WhoseValue.Should().Be("hello");
        }

        [Fact(DisplayName = "key:version \"value\" が正しく読み込まれる")]
        public void ParseText_KeyVersionValue_IsParsed()
        {
            var text = """
            greeting:0 "hello"
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("greeting").WhoseValue.Should().Be("hello");
        }

        [Fact(DisplayName = "ヘッダー行(l_japanese:)や空行がスキップされる")]
        public void ParseText_HeaderAndEmptyLines_AreSkipped()
        {
            var text = """
            l_japanese:

            greeting:0 "hello"

            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("greeting").WhoseValue.Should().Be("hello");
            result.Should().NotContainKey("l_japanese");
        }

        [Fact(DisplayName = "コメント行がスキップされる")]
        public void ParseText_CommentLines_AreSkipped()
        {
            var text = """
            # comment 1
            greeting:0 "hello"
            # comment 2
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("greeting").WhoseValue.Should().Be("hello");
        }

        [Fact(DisplayName = "value中の\\\\や\\\"が正しく認識される")]
        public void ParseText_EscapedCharacters_AreParsed()
        {
            var text = """
            path:0 "C:\\Program Files\\Victoria3"
            quote:0 "He said: \"hello\""
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(2);
            result.Should().ContainKey("path").WhoseValue.Should().Be(@"C:\Program Files\Victoria3");
            result.Should().ContainKey("quote").WhoseValue.Should().Be("He said: \"hello\"");
        }

        [Fact(DisplayName = "valueの後にコメントがあっても正しく認識される")]
        public void ParseText_TrailingComment_IsAllowed()
        {
            var text = """
            greeting:0 "hello" # trailing comment
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("greeting").WhoseValue.Should().Be("hello");
        }

        [Fact(DisplayName = "コロンがない場合スキップされる")]
        public void ParseText_NoColon_IsSkipped()
        {
            var text = """
            invalid line
            valid:0 "ok"
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("valid").WhoseValue.Should().Be("ok");
        }

        [Fact(DisplayName = "キーが空文字列のときスキップされる")]
        public void ParseText_EmptyKey_IsSkipped()
        {
            var text = """
            :0 "value"
            valid:0 "ok"
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("valid").WhoseValue.Should().Be("ok");
            result.Should().NotContainKey(string.Empty);
        }

        [Fact(DisplayName = "valueの開始引用符がないとスキップされる")]
        public void ParseText_MissingOpeningQuote_IsSkipped()
        {
            var text = """
            invalid:0 value
            valid:0 "ok"
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("valid").WhoseValue.Should().Be("ok");
            result.Should().NotContainKey("invalid");
        }

        [Fact(DisplayName = "終了引用符がないとスキップされる")]
        public void ParseText_MissingClosingQuote_IsSkipped()
        {
            var text = """
            invalid:0 "value
            valid:0 "ok"
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("valid").WhoseValue.Should().Be("ok");
            result.Should().NotContainKey("invalid");
        }

        [Fact(DisplayName = "valueの後にコメントでない文字列があるときスキップされる")]
        public void ParseText_TrailingNonComment_IsSkipped()
        {
            var text = """
            invalid:0 "value" trailing
            valid:0 "ok"
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("valid").WhoseValue.Should().Be("ok");
            result.Should().NotContainKey("invalid");
        }

        [Fact(DisplayName = "重複キーは後勝ちで上書きされる")]
        public void ParseText_DuplicateKeys_LastWins()
        {
            var text = """
            key:0 "first"
            key:0 "second"
            """;

            var result = LocalizationParser.ParseText(text);

            result.Should().HaveCount(1);
            result.Should().ContainKey("key").WhoseValue.Should().Be("second");
        }
    }
}
```

