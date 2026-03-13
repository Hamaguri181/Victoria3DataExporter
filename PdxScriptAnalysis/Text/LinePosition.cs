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
