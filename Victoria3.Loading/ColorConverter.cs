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
        /// 指定された RGB 値を使用して <see cref="GameColor"/> を作成する。
        /// RGB 値は 0-255 の範囲であるときと、0-1 の範囲であるときの両方に対応する。
        /// </summary>
        /// <param name="r">赤成分 (0-255)</param>
        /// <param name="g">緑成分 (0-255)</param>
        /// <param name="b">青成分 (0-255)</param>
        /// <returns>作成された <see cref="GameColor"/> オブジェクト</returns>
        internal static GameColor FromRgb(decimal r, decimal g, decimal b)
        {
            if (r <= 1 && g <= 1 && b <= 1)
            {
                r *= 255; g *= 255; b *= 255;
            }
            return FromRgb((byte)r, (byte)g, (byte)b);
        }

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
