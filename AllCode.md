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
        /// 解析中にエラーが発生したかどうか。
        /// </summary>
        public bool HasErrors => Diagnostics.Any(d => d.IsError);


        // コンストラクタはprivateで、ファクトリメソッドを通じてのみインスタンス化される。
        private ScriptTree(SourceText source, RootNode root, IReadOnlyList<Diagnostic> diagnostics)
        {
            Source = source;
            Root = root;
            Diagnostics = diagnostics;
        }


        /// <summary>
        /// ファイルから解析を行うファクトリメソッド。指定されたファイルパスからソーステキストを読み込み、解析を行い、ScriptTreeのインスタンスを生成する。
        /// </summary>
        /// <param name="path">解析対象のファイルパス</param>
        /// <returns>解析結果を表すScriptTreeのインスタンス</returns>
        public static ScriptTree ParseFile(string path)
            => ParseCore(SourceText.FromFile(path));

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
    /// <param name="LinePosition">トークンの行位置情報</param>
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
    public sealed record Diagnostic(
        DiagnosticSeverity Severity,
        string Message,
        TextSpan Span,
        LinePosition LinePosition)
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
                => $"{Severity}: {Message} at {LinePosition}";
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

            if (_position >= _source.Length) return MakeToken(SyntaxKind.EndOfFile, _position, 0);

            return Current switch
            {
                '{' => MakeToken(SyntaxKind.LeftBrace, _position++, 1),
                '}' => MakeToken(SyntaxKind.RightBrace, _position++, 1),
                '=' => MakeToken(SyntaxKind.Equals, _position++, 1),
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
                return MakeToken(SyntaxKind.LessThanEquals, start, 2);
            }
            return MakeToken(SyntaxKind.LessThan, start, 1);
        }

        private SyntaxToken ReadGreaterThan()
        {
            var start = _position;
            Advance();
            if (_position < _source.Length && Current == '=')
            {
                Advance();
                return MakeToken(SyntaxKind.GreaterThanEquals, start, 2);
            }
            return MakeToken(SyntaxKind.GreaterThan, start, 1);
        }

        private SyntaxToken ReadNotEquals()
        {
            var start = _position;
            Advance();
            if (_position < _source.Length && Current == '=')
            {
                Advance();
                return MakeToken(SyntaxKind.NotEquals, start, 2);
            }
            return MakeToken(SyntaxKind.Unknown, start, 1);
        }

        private SyntaxToken ReadQuestionEquals()
        {
            var start = _position;
            Advance();
            if (_position < _source.Length && Current == '=')
            {
                Advance();
                return MakeToken(SyntaxKind.QuestionEquals, start, 2);
            }
            return MakeToken(SyntaxKind.Unknown, start, 1);
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
            if (_position >= _source.Length) return MakeToken(SyntaxKind.Unknown, start, _position - start);

            if (_position < _source.Length)
            {
                Advance(); // 終了の二重引用符をスキップ
            }
            return MakeToken(SyntaxKind.StringLiteral, start, _position - start);
        }

        private SyntaxToken ReadAtom()
        {
            var start = _position;
            while (_position < _source.Length && IsAtomChar(Current))
            {
                Advance();
            }

            if (start == _position) return MakeToken(SyntaxKind.Unknown, _position++, 1);

            return MakeToken(SyntaxKind.Atom, start, _position - start);
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

        private SyntaxToken MakeToken(SyntaxKind kind, int start, int length)
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
    /// </summary>
    public sealed class SourceText
    {
        private readonly string _text;


        private SourceText(string text)
        {
            _text = text;
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
            return From(content);
        }


        /// <summary>
        /// ソーステキストの長さ。
        /// </summary>
        public int Length => _text.Length;

        /// <summary>
        /// ソーステキストの指定した位置の文字。インデックスが範囲外の場合はIndexOutOfRangeExceptionになる。
        /// </summary>
        /// <param name="index">取得する文字のインデックス。</param>
        /// <returns>指定したインデックスの文字。</returns>
        public char this[int index] => _text[index];


        /// <summary>
        /// テキストスパンに対応する部分文字列を含む新しい<see cref="SourceText"/>を返す。スパンが範囲外の場合はArgumentOutOfRangeExceptionになる。
        /// </summary>
        /// <param name="span">取得する部分文字列の範囲を表す<see cref="TextSpan"/>。</param>
        /// <returns>指定した範囲の部分文字列を含む新しい<see cref="SourceText"/>。</returns>
        /// <exception cref="ArgumentOutOfRangeException">spanが範囲外の場合にスローされる。</exception>
        public string GetSubText(TextSpan span)
        {
            if (span.End > Length) throw new ArgumentOutOfRangeException(nameof(span), "TextSpan is out of range.");
            var spanLength = span.Length;
            return span.IsEmpty ? string.Empty :
                spanLength == Length ? _text :
                _text.Substring(span.Start, spanLength);
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
                if (_text[i] == '\r')
                {
                    continue;
                }
                else if (_text[i] == '\n')
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

                if (_text[i] == '\r')
                {
                    continue;
                }
                else if (_text[i] == '\n')
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

        public override string ToString() => _text;
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

            Assert.True(tree.HasErrors);
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
            Assert.True(tree.HasErrors);
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
            Assert.True(tree.HasErrors);
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
            Assert.True(tree.HasErrors);
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
            Assert.False(tree.HasErrors);
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

                var countryDataPath = Path.Combine(gameDir, Victoria3Paths.CountryDefinitions);

                var scriptTrees = Directory.EnumerateFiles(countryDataPath, "*.txt").Select(ScriptTree.ParseFile).ToList();

                Console.WriteLine($"ファイル\"{countryDataPath}\"を解析しました。診断結果: {scriptTrees.Sum(st => st.Diagnostics.Count)}件");

                var output = new CountryLoader(scriptTrees).Load();

                Console.WriteLine($"読み込んだ国の数: {output.Values.Count}、診断結果: {output.Diagnostics.Count}件");
                var localizationPath = Path.Combine(gameDir, LocalizationPaths.Japanese);
                var localizer = FileLocalizer.FromDirectory(localizationPath);

                foreach (var (index, country) in output.Values.Index())
                {
                    Console.WriteLine($"{index + 1,-4}: タグ: {country.Tag}, 名前: {localizer.Localize(country.Tag)}, 種別: {country.Type}, ティア: {country.Tier}");
                }

                foreach (var diagnostic in output.Diagnostics)
                {
                    Console.WriteLine($"診断結果: {diagnostic.Message} at {diagnostic.LinePosition}");
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
        GameColor? TertiaryUnitColor);
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
        byte B);
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
        public const string CountryDefinitions = "common/country_definitions";
    }
}
```

```Victoria3.Loading\Loaders\CountryLoader.cs
using PdxScriptAnalysis;
using PdxScriptAnalysis.Diagnostics;
using PdxScriptAnalysis.Syntax;
using PdxScriptAnalysis.Text;
using System.Diagnostics.CodeAnalysis;
using Victoria3.GameData;

namespace Victoria3.Loading.Loaders
{
    /// <summary>
    /// 国家データを <see cref="ScriptTree"/> から読み込むローダー。
    /// </summary>
    /// <param name="trees">読み込むスクリプトツリーのコレクション。</param>
    public sealed class CountryLoader(IEnumerable<ScriptTree> trees)
    {
        private readonly IEnumerable<ScriptTree> _trees = trees;
        private readonly List<Diagnostic> _diagnostics = [];

        /// <summary>
        /// 国家データをスクリプトツリーから読み込むメソッド。各ツリーを処理し、国家データのリストと診断情報を含む <see cref="LoadOutput{Country}"/> を返す。
        /// </summary>
        /// <returns>読み込まれた国家データと診断情報を含む <see cref="LoadOutput{Country}"/> オブジェクト</returns>
        public LoadOutput<Country> Load()
        {
            _diagnostics.Clear();
            var countries = new List<Country>();

            foreach (var tree in _trees)
            {
                var countriesFromTree = LoadFromTree(tree);
                countries.AddRange(countriesFromTree);
            }

            return new LoadOutput<Country>(countries, _diagnostics);
        }

        private List<Country> LoadFromTree(ScriptTree tree)
        {
            var countries = new List<Country>();

            foreach (var topLevelNode in tree.Root.Children)
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

            return countries;
        }

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
                        if (TryParseToGameColor(propertyNode, "color", out var color))
                        {
                            countryBuilder.Color = color;
                        }
                        break;
                    case "country_type":
                        if (TryParseToString(propertyNode, "country_type", out var typeValue))
                        {
                            var originalTypeValue = typeValue;
                            typeValue = typeValue.Replace("_", "", StringComparison.OrdinalIgnoreCase);
                            if (Enum.TryParse<CountryType>(typeValue, ignoreCase: true, out var type))
                            {
                                countryBuilder.Type = type;
                            }
                            else
                            {
                                AddError($"Invalid value \"{originalTypeValue}\" for property \"type\". Expected one of the following values: {string.Join(", ", Enum.GetNames<CountryType>())}.", propertyNode.Span, propertyNode.LinePosition);
                            }
                        }
                        break;
                    case "tier":
                        if (TryParseToString(propertyNode, "tier", out var tierValue))
                        {
                            var originalTierValue = tierValue;
                            tierValue = tierValue.Replace("_", "", StringComparison.OrdinalIgnoreCase);
                            if (Enum.TryParse<CountryTier>(tierValue, ignoreCase: true, out var tier))
                            {
                                countryBuilder.Tier = tier;
                            }
                            else
                            {
                                AddError($"Invalid value \"{originalTierValue}\" for property \"tier\". Expected one of the following values: {string.Join(", ", Enum.GetNames<CountryTier>())}.", propertyNode.Span, propertyNode.LinePosition);
                            }
                        }
                        break;
                    case "social_hierarchy":
                        if (TryParseToString(propertyNode, "social_hierarchy", out var socialHierarchy))
                        {
                            countryBuilder.SocialHierarchy = socialHierarchy;
                        }
                        break;
                    case "religion":
                        if (TryParseToString(propertyNode, "religion", out var religion))
                        {
                            countryBuilder.Religion = religion;
                        }
                        break;
                    case "cultures":
                        if (TryParseToStringList(propertyNode, "cultures", out var cultures))
                        {
                            countryBuilder.Cultures = cultures;
                        }
                        break;
                    case "capital":
                        if (TryParseToString(propertyNode, "capital", out var capital))
                        {
                            countryBuilder.Capital = capital;
                        }
                        break;
                    case "is_named_from_capital":
                        if (TryParseToBool(propertyNode, "is_named_from_capital", out var isNamedFromCapital))
                        {
                            countryBuilder.IsNamedFromCapital = isNamedFromCapital;
                        }
                        break;
                    case "valid_as_home_country_for_separatists":
                        // 一旦ノードをそのまま
                        countryBuilder.ValidAsHomeCountryForSeparatists = propertyNode;
                        break;
                    case "primary_unit_color":
                        if (TryParseToGameColor(propertyNode, "primary_unit_color", out var primaryUnitColor))
                        {
                            countryBuilder.PrimaryUnitColor = primaryUnitColor;
                        }
                        break;
                    case "secondary_unit_color":
                        if (TryParseToGameColor(propertyNode, "secondary_unit_color", out var secondaryUnitColor))
                        {
                            countryBuilder.SecondaryUnitColor = secondaryUnitColor;
                        }
                        break;
                    case "tertiary_unit_color":
                        if (TryParseToGameColor(propertyNode, "tertiary_unit_color", out var tertiaryUnitColor))
                        {
                            countryBuilder.TertiaryUnitColor = tertiaryUnitColor;
                        }
                        break;
                    case "dynamic_country_definition":
                        // dynamic_country_definition = yes のプロパティを持つ場合その国家は読み取らない
                        if (TryParseToBool(propertyNode, "dynamic_country_definition", out var isDynamicCountryDefinition) && isDynamicCountryDefinition == true)
                        {
                            country = default!;
                            return false;
                        }
                        break;
                    default:
                        AddWarning($"Unexpected property \"{propertyNode.Key.Text}\" in country definition. This property will be ignored.", propertyNode.Key.Span, propertyNode.LinePosition);
                        break;
                }
            }

            var missings = countryBuilder.GetMissingRequiredProperties();
            if (missings.Count > 0)
            {
                AddError($"Missing required properties for country with tag \"{tag}\": {string.Join(", ", missings)}.", node.Span, node.LinePosition);
                country = default!;
                return false;
            }

            country = countryBuilder.Build();
            return true;
        }


        // スカラープロパティノードの右辺を文字列として解析するためのヘルパーメソッド
        private bool TryParseToString(PropertyNode node, string propertyName, [NotNullWhen(true)] out string value)
        {
            if (node is not ScalarPropertyNode scalarPropertyNode)
            {
                AddError($"Expected a scalar property node for property \"{propertyName}\", but found a different type of node.", node.Span, node.LinePosition);
                value = null!;
                return false;
            }

            value = scalarPropertyNode.Value.Token.Text;
            return true;
        }

        // ブロックプロパティノードの右辺を文字列のリストとして解析するためのヘルパーメソッド
        private bool TryParseToStringList(PropertyNode node, string propertyName, [NotNullWhen(true)] out List<string> values)
        {
            if (node is not BlockPropertyNode blockPropertyNode)
            {
                AddError($"Expected a block property node for property \"{propertyName}\", but found a different type of node.", node.Span, node.LinePosition);
                values = null!;
                return false;
            }

            if (blockPropertyNode.Value.Children.Any(c => c is not ScalarNode))
            {
                AddError($"Expected all children of the block for property \"{propertyName}\" to be scalar nodes representing string values, but found child nodes of different types.", blockPropertyNode.Span, blockPropertyNode.LinePosition);
                values = null!;
                return false;
            }

            values = blockPropertyNode.Value.Children
                .OfType<ScalarNode>()
                .Select(n => n.Token.Text)
                .ToList();
            return true;
        }

        // スカラープロパティノードの右辺を真偽値として解析するためのヘルパーメソッド
        private bool TryParseToBool(PropertyNode node, string propertyName, out bool value)
        {
            if (node is not ScalarPropertyNode scalarPropertyNode)
            {
                AddError($"Expected a scalar property node for property \"{propertyName}\", but found a different type of node.", node.Span, node.LinePosition);
                value = default;
                return false;
            }

            switch (scalarPropertyNode.Value.Token.Text)
            {
                case "yes":
                    value = true;
                    return true;
                case "no":
                    value = false;
                    return true;
                default:
                    AddError($"Expected the value of property \"{propertyName}\" to be \"yes\" or \"no\", but found \"{scalarPropertyNode.Value.Token.Text}\".", scalarPropertyNode.Value.Span, scalarPropertyNode.Value.LinePosition);
                    value = default;
                    return false;
            }
        }

        // ブロックプロパティノードまたは型付きブロックプロパティノードの右辺を GameColor として解析するためのヘルパーメソッド
        private bool TryParseToGameColor(PropertyNode node, string propertyName, out GameColor color)
        {
            if (node is BlockPropertyNode block)
            {
                if (TryParseFromBlockToGameColor(block.Value, propertyName, out var colorValues))
                {
                    color = ColorConverter.FromRgb(colorValues[0], colorValues[1], colorValues[2]);
                    return true;
                }
                else
                {
                    color = default;
                    return false;
                }
            }
            else if (node is TypedBlockPropertyNode typedBlock)
            {
                if (!TryParseFromBlockToGameColor(typedBlock.Value, propertyName, out var typedColorValues))
                {
                    color = default;
                    return false;
                }


                var typeQualifier = typedBlock.TypeQualifier.Text;
                if (typeQualifier.Equals("hsv", StringComparison.OrdinalIgnoreCase))
                {
                    color = ColorConverter.FromHsv(typedColorValues[0], typedColorValues[1], typedColorValues[2]);
                    return true;
                }
                else if (typeQualifier.Equals("hsv360", StringComparison.OrdinalIgnoreCase))
                {
                    color = ColorConverter.FromHsv360(typedColorValues[0], typedColorValues[1], typedColorValues[2]);
                    return true;
                }
                else if (typeQualifier.Equals("rgb", StringComparison.OrdinalIgnoreCase))
                {
                    color = ColorConverter.FromRgb(typedColorValues[0], typedColorValues[1], typedColorValues[2]);
                    return true;
                }
                else
                {
                    AddError($"Unsupported color type qualifier \"{typeQualifier}\" for property \"{propertyName}\". Expected \"rgb\", \"hsv\", or \"hsv360\".", typedBlock.TypeQualifier.Span, typedBlock.TypeQualifier.LinePosition);
                    color = default;
                    return false;
                }
            }
            else
            {
                AddError($"Expected a block or typed block property node for property \"{propertyName}\", but found a different type of node.", node.Span, node.LinePosition);
                color = default;
                return false;
            }
        }

        // ブロックノードの子ノードを色の値として解析するためのヘルパーメソッド
        private bool TryParseFromBlockToGameColor(BlockNode block, string propertyName, out decimal[] colorValues)
        {
            if (block.Children.Count != 3)
            {
                AddError($"Expected a block with exactly 3 children for property \"{propertyName}\" to represent RGB values, but found a block with {block.Children.Count} children.", block.Span, block.LinePosition);
                colorValues = [];
                return false;
            }

            if (block.Children.Any(c => c is not ScalarNode))
            {
                AddError($"Expected all children of the block for property \"{propertyName}\" to be scalar nodes representing RGB components, but found a child node of a different type.", block.Span, block.LinePosition);
                colorValues = [];
                return false;
            }

            var rgbValueNodes = block.Children.OfType<ScalarNode>().ToList();

            colorValues = new decimal[3];
            for (int i = 0; i < 3; i++)
            {
                if (!decimal.TryParse(rgbValueNodes[i].Token.Text, out colorValues[i]))
                {
                    AddError($"Expected the value of child node {i + 1} of the block for property \"{propertyName}\" to be a valid byte (0-255) representing an RGB component, but found \"{rgbValueNodes[i].Token.Text}\".", rgbValueNodes[i].Span, rgbValueNodes[i].LinePosition);
                    colorValues = [];
                    return false;
                }
            }
            return true;
        }

        // エラー診断を追加するためのヘルパーメソッド
        private void AddError(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, message, span, linePosition));

        private void AddWarning(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, message, span, linePosition));

        // 国のビルダークラス。必須プロパティを null 許容型で保持し、ビルド時に不足しているプロパティをチェックする。
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

```Victoria3.Loading.Tests\Loaders\CountryLoaderErrorTests.cs
using FluentAssertions;
using PdxScriptAnalysis;
using Victoria3.Loading.Loaders;

namespace Victoria3.Loading.Tests.Loaders
{
    public class CountryLoaderErrorTests
    {

        // テスト用の ScriptTree を生成するヘルパーメソッド
        private static ScriptTree ParseTree(string text)
            => ScriptTree.ParseText(text);

        // 複数の ScriptTree を生成するヘルパーメソッド
        private static IEnumerable<ScriptTree> ParseTrees(params string[] texts)
            => texts.Select(ScriptTree.ParseText);


        // --- 必須フィールド欠損 ---

        [Fact(DisplayName = "Color が欠損している場合、エラーが返される")]
        public void Load_MissingColor_ReturnsErrorAndNoCountry()
        {
            var input = """
            GER = {
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
            output.Diagnostics[0].Message.Should().Contain("Color");
        }

        [Fact(DisplayName = "Type が欠損している場合、エラーが返される")]
        public void Load_MissingType_ReturnsErrorAndNoCountry()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
            output.Diagnostics[0].Message.Should().Contain("Type");
        }

        [Fact(DisplayName = "Tier が欠損している場合、エラーが返される")]
        public void Load_MissingTier_ReturnsErrorAndNoCountry()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
            output.Diagnostics[0].Message.Should().Contain("Tier");
        }

        [Fact(DisplayName = "Cultures が欠損している場合、エラーが返される")]
        public void Load_MissingCultures_ReturnsErrorAndNoCountry()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                capital = STATE_BRANDENBURG
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
            output.Diagnostics[0].Message.Should().Contain("Cultures");
        }

        [Fact(DisplayName = "複数の必須フィールドが欠損している場合、エラーメッセージにすべての欠損フィールドが含まれる")]
        public void Load_MultipleRequiredFieldsMissing_ErrorMessageContainsAllMissingFields()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);

            var message = output.Diagnostics[0].Message;
            message.Should().Contain("Tier");
            message.Should().Contain("Cultures");
        }

        // --- 不正な値 ---

        [Fact(DisplayName = "不正な CountryType が指定されている場合、エラーが返される")]
        public void Load_UnknownCountryType_ReturnsError()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = republic
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("republic"));
        }

        [Fact(DisplayName = "不正な CountryTier が指定されている場合、エラーが返される")]
        public void Load_UnknownTier_ReturnsError()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = duchy
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("duchy"));
        }

        [Fact(DisplayName = "不正な is_named_from_capital が指定されている場合、エラーが返される")]
        public void Load_InvalidIsNamedFromCapital_ReturnsError()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
                is_named_from_capital = true
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            // is_named_from_capital のエラーが記録されること
            output.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("is_named_from_capital"));
        }

        // --- 色ブロックのエラー ---

        [Fact(DisplayName = "Color ブロックの要素数が不正な場合、エラーが返される")]
        public void Load_ColorBlockWithWrongElementCount_ReturnsError()
        {
            var input = """
            GER = {
                color = { 147 130 }
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("color"));
        }

        [Fact(DisplayName = "Color ブロックに数値以外の値が含まれている場合、エラーが返される")]
        public void Load_ColorBlockWithNonNumericValue_ReturnsError()
        {
            var input = """
            GER = {
                color = { 147 abc 110 }
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("color"));
        }

        [Fact(DisplayName = "不正な Color クオリファイアが指定されている場合、エラーが返される")]
        public void Load_UnknownColorQualifier_ReturnsError()
        {
            var input = """
            GER = {
                color = hsl { 0.5 0.8 0.9 }
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("hsl"));
        }

        // --- トップレベルノードのエラー ---

        [Fact(DisplayName = "トップレベルノードがスカラー値の場合、エラーが返される")]
        public void Load_TopLevelScalarNode_ReturnsError()
        {
            var input = "some_scalar_value";

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
        }

        // --- 失敗エントリと成功エントリの混在 ---

        [Fact(DisplayName = "有効な国と無効な国が混在している場合、有効な国のみが返される")]
        public void Load_ValidAndInvalidCountries_ReturnsOnlyValidCountries()
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

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().HaveCount(2);
            output.Values.Select(c => c.Tag).Should().Equal("GER", "FRA");
            output.Diagnostics.Should().ContainSingle(d => d.IsError);
        }
    }

    public class CountryLoaderWarningTests
    {

        // テスト用の ScriptTree を生成するヘルパーメソッド
        private static ScriptTree ParseTree(string text)
            => ScriptTree.ParseText(text);

        // 複数の ScriptTree を生成するヘルパーメソッド
        private static IEnumerable<ScriptTree> ParseTrees(params string[] texts)
            => texts.Select(ScriptTree.ParseText);


        [Fact(DisplayName = "不明なプロパティが指定されている場合、警告が返される")]
        public void Load_UnknownProperty_ReturnsWarningAndLoadsCountry()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
                flag_color = red
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            // ロードは成功する
            output.Values.Should().HaveCount(1);
            output.Values[0].Tag.Should().Be("GER");

            // 警告が1件記録される
            output.Diagnostics.Should().ContainSingle(d => d.IsWarning);
            output.Diagnostics[0].Message.Should().Contain("flag_color");
        }

        [Fact(DisplayName = "複数の不明なプロパティが指定されている場合、各プロパティに対して警告が返される")]
        public void Load_MultipleUnknownProperties_ReturnsWarningsForEach()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                cultures = { north_german }
                capital = STATE_BRANDENBURG
                flag_color = red
                unknown_prop = foo
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().HaveCount(1);
            output.Diagnostics.Should().HaveCount(2);
            output.Diagnostics.Should().AllSatisfy(d => d.IsWarning.Should().BeTrue());
        }
    }
}
```

```Victoria3.Loading.Tests\Loaders\CountryLoaderSuccessTests.cs
using FluentAssertions;
using PdxScriptAnalysis;
using Victoria3.GameData;
using Victoria3.Loading.Loaders;

namespace Victoria3.Loading.Tests.Loaders
{
    public class CountryLoaderSuccessTests
    {
        // 最小構成の国家定義（必須フィールドのみ）
        private const string MinimalCountry = """
        GER = {
            color = { 147 130 110 }
            country_type = recognized
            tier = empire
            cultures = { north_german }
            capital = STATE_BRANDENBURG
        }
        """;

        // テスト用の ScriptTree を生成するヘルパーメソッド
        private static ScriptTree ParseTree(string text)
            => ScriptTree.ParseText(text);

        // 複数の ScriptTree を生成するヘルパーメソッド
        private static IEnumerable<ScriptTree> ParseTrees(params string[] texts)
            => texts.Select(ScriptTree.ParseText);



        // 正常系

        [Fact(DisplayName = "最小構成の国家データを読み込むと、正しいタグを持つ国家が返される")]
        public void Load_MinimalCountry_ReturnsCountryWithCorrectTag()
        {
            var loader = new CountryLoader(ParseTrees(MinimalCountry));
            var output = loader.Load();

            output.Values.Should().HaveCount(1);
            output.Values[0].Tag.Should().Be("GER");
        }

        [Fact(DisplayName = "最小構成の国家データを読み込むと、エラーが発生しない")]
        public void Load_MinimalCountry_ReturnsNoErrors()
        {
            var loader = new CountryLoader(ParseTrees(MinimalCountry));
            var output = loader.Load();

            output.Diagnostics.Should().BeEmpty();
        }

        [Fact(DisplayName = "最小構成の国家データを読み込むと、必須フィールドが正しく解析される")]
        public void Load_MinimalCountry_ParsesRequiredFields()
        {
            var loader = new CountryLoader(ParseTrees(MinimalCountry));
            var output = loader.Load();

            var country = output.Values[0];
            country.Type.Should().Be(CountryType.Recognized);
            country.Tier.Should().Be(CountryTier.Empire);
            country.Cultures.Should().Equal("north_german");
            country.Capital.Should().Be("STATE_BRANDENBURG");
        }

        [Fact(DisplayName = "最小構成の国家データを読み込むと、オプションフィールドがデフォルト値を持つ")]
        public void Load_MinimalCountry_OptionalFieldsHaveDefaults()
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

        [Fact(DisplayName = "すべてのオプションフィールドが正しく解析される")]
        public void Load_AllOptionalFields_ParsesCorrectly()
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
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            var country = output.Values[0];
            country.SocialHierarchy.Should().Be("monarchy");
            country.Religion.Should().Be("shinto");
            country.IsNamedFromCapital.Should().BeTrue();
        }

        [Fact(DisplayName = "複数の文化を持つ国家データを読み込むと、すべての文化が正しく解析される")]
        public void Load_MultipleCultures_ParsesAllCultures()
        {
            var input = """
            GER = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                cultures = { north_german south_german }
                capital = STATE_BRANDENBURG
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values[0].Cultures.Should().Equal("north_german", "south_german");
        }

        [Fact(DisplayName = "1つのスクリプトツリーに複数の国家データが含まれる場合、すべての国家が返される")]
        public void Load_MultipleCountriesInOneTree_ReturnsAll()
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

            output.Values.Should().HaveCount(2);
            output.Values.Select(c => c.Tag).Should().Equal("GER", "FRA");
        }

        [Fact(DisplayName = "複数のスクリプトツリーに複数の国家データが含まれる場合、すべての国家が返される")]
        public void Load_MultipleScriptTrees_ReturnsAllCountries()
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

            output.Values.Should().HaveCount(2);
            output.Values.Select(c => c.Tag).Should().Equal("GER", "FRA");
        }

        // --- CountryType のパース ---

        [Theory(DisplayName = "すべての CountryType が正しく解析される")]
        [InlineData("recognized", CountryType.Recognized)]
        [InlineData("colonial", CountryType.Colonial)]
        [InlineData("unrecognized", CountryType.Unrecognized)]
        [InlineData("decentralized", CountryType.Decentralized)]
        public void Load_AllCountryTypes_ParsesCorrectly(string typeText, CountryType expected)
        {
            var input = $$"""
            X = {
                color = { 0 0 0 }
                country_type = {{typeText}}
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values[0].Type.Should().Be(expected);
        }

        // --- CountryTier のパース ---

        [Theory(DisplayName = "すべての CountryTier が正しく解析される")]
        [InlineData("hegemony", CountryTier.Hegemony)]
        [InlineData("empire", CountryTier.Empire)]
        [InlineData("kingdom", CountryTier.Kingdom)]
        [InlineData("grand_principality", CountryTier.GrandPrincipality)]
        [InlineData("principality", CountryTier.Principality)]
        [InlineData("city_state", CountryTier.CityState)]
        public void Load_AllCountryTiers_ParsesCorrectly(string tierText, CountryTier expected)
        {
            var input = $$"""
            X = {
                color = { 0 0 0 }
                country_type = recognized
                tier = {{tierText}}
                cultures = { foo }
                capital = STATE_X
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values[0].Tier.Should().Be(expected);
        }

        // --- 色形式のパース ---

        [Fact(DisplayName = "RGB ブロック形式の色が正しく解析される")]
        public void Load_ColorRgbBlock_ParsesCorrectly()
        {
            var input = """
            X = {
                color = { 147 130 110 }
                country_type = recognized
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values[0].Color.Should().Be(new GameColor(147, 130, 110));
        }

        [Fact(DisplayName = "RGB 型ブロック形式の色が正しく解析される")]
        public void Load_ColorRgbTypedBlock_ParsesCorrectly()
        {
            var input = """
            X = {
                color = rgb { 147 130 110 }
                country_type = recognized
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values[0].Color.Should().Be(new GameColor(147, 130, 110));
        }

        [Fact(DisplayName = "HSV 型ブロック形式の色が正しく解析される")]
        public void Load_ColorHsv_ConvertsToRgb()
        {
            // hsv { 0.0 0.0 1.0 } = 白 (255, 255, 255)
            var input = """
            X = {
                color = hsv { 0.0 0.0 1.0 }
                country_type = recognized
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values[0].Color.Should().Be(new GameColor(255, 255, 255));
        }

        [Fact(DisplayName = "HSV360 型ブロック形式の色が正しく解析される")]
        public void Load_ColorHsv360_ConvertsToRgb()
        {
            // hsv360 { 0 0 100 } = 白 (255, 255, 255)
            var input = """
            X = {
                color = hsv360 { 0 0 100 }
                country_type = recognized
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values[0].Color.Should().Be(new GameColor(255, 255, 255));
        }

        // --- dynamic_country_definition ---

        [Fact(DisplayName = "dynamic_country_definition が yes の場合、国家データはスキップされる")]
        public void Load_DynamicCountryDefinition_IsSkipped()
        {
            var input = """
            DYN = {
                dynamic_country_definition = yes
                color = { 0 0 0 }
                country_type = recognized
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().BeEmpty();
            output.Diagnostics.Should().BeEmpty();
        }

        [Fact(DisplayName = "dynamic_country_definition が no の場合、国家データはスキップされない")]
        public void Load_DynamicCountryDefinitionNo_IsNotSkipped()
        {
            var input = """
            X = {
                dynamic_country_definition = no
                color = { 0 0 0 }
                country_type = recognized
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            var output = loader.Load();

            output.Values.Should().HaveCount(1);
        }

        // --- Load() の再呼び出し ---

        [Fact(DisplayName = "Load() を再呼び出しした場合、診断がリセットされる")]
        public void Load_CalledTwice_DiagnosticsAreReset()
        {
            var input = """
            X = {
                color = { 0 0 0 }
                country_type = recognized
                tier = empire
                cultures = { foo }
                capital = STATE_X
            }
            """;

            var loader = new CountryLoader(ParseTrees(input));
            loader.Load();
            var output = loader.Load();

            // 2回目も同じ結果になること（診断がクリアされること）
            output.Values.Should().HaveCount(1);
            output.Diagnostics.Should().BeEmpty();
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

```Victoria3.Localization\LocalizationPaths.cs
namespace Victoria3.Localization
{
    /// <summary>
    /// 翻訳ファイルのパスを定義するクラス。
    /// </summary>
    public static class LocalizationPaths
    {
        public const string Japanese = "localization/japanese";
        public const string English = "localization/english";
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

