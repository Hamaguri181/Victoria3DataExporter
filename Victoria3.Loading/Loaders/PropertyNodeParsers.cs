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
                .Replace("-", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" ", "", StringComparison.OrdinalIgnoreCase);

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
