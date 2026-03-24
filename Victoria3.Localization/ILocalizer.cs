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
