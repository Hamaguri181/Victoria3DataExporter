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
                    AddError("Unexpected end of file. Expected '}' to close the block.", Current.Span);
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
                AddError($"Invalid property value: \"{Current.Text}\"", Current.Span);
                var errorToken = CreateMissing(Current.Span);
                var errorScalar = new ScalarNode(errorToken, errorToken.Span);
                var span = TextSpan.Union(key.Span, errorScalar.Span);
                return new ScalarPropertyNode(key, op, errorScalar, span);
            }
        }

        // 予期しないトークンが出現した場合のエラーハンドリング
        private SyntaxNode? ParseUnexpected()
        {
            AddError($"Unexpected token: \"{Current.Text}\"", Current.Span);
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
        private static SyntaxToken CreateMissing(TextSpan span)
            => new(SyntaxKind.Unknown, string.Empty, new TextSpan(span.Start, 0));

        // エラー診断を追加するためのヘルパーメソッド
        private void AddError(string message, TextSpan span)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, message, span));
    }
}
