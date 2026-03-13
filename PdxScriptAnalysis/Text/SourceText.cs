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
