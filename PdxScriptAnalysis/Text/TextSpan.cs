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
