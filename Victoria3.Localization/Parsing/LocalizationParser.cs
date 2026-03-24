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
