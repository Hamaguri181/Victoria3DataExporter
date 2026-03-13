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
            return new SyntaxToken(kind, text, span);
        }
    }
}