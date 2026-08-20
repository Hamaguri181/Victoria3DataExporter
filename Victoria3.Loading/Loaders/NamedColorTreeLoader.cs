using PdxScriptAnalysis;
using PdxScriptAnalysis.Diagnostics;
using PdxScriptAnalysis.Syntax;
using PdxScriptAnalysis.Text;
using Victoria3.GameData;

namespace Victoria3.Loading.Loaders
{
    internal sealed class NamedColorTreeLoader(ScriptTree tree)
    {
        private readonly ScriptTree _tree = tree;
        private readonly List<Diagnostic> _diagnostics = [];

        private string? FilePath => _tree.Source.FilePath;

        internal LoadOutput<NamedColor> Load()
        {
            var colors = new List<NamedColor>();

            if (_tree.Root.Children.Count != 1)
            {
                AddError("Expected exactly one top-level node representing named colors definition in one script tree.", _tree.Root.Span, _tree.Root.LinePosition);
                return new LoadOutput<NamedColor>(colors, _diagnostics);
            }
            var topLevelNode = _tree.Root.Children[0];
            if (topLevelNode is not BlockPropertyNode blockPropertyNode)
            {
                AddError($"Unexpected top-level node of type {topLevelNode.GetType().Name}. Expected a BlockPropertyNode representing named colors definition.", topLevelNode.Span, topLevelNode.LinePosition);
                return new LoadOutput<NamedColor>(colors, _diagnostics);
            }
            if (blockPropertyNode.Key.Text != "colors")
            {
                AddError($"Unexpected top-level block key '{blockPropertyNode.Key.Text}'. Expected 'colors' for named colors definition.", blockPropertyNode.Span, blockPropertyNode.LinePosition);
                return new LoadOutput<NamedColor>(colors, _diagnostics);
            }

            foreach (var childNode in blockPropertyNode.Value.Children)
            {
                if (childNode is not PropertyNode propertyNode)
                {
                    AddError($"Unexpected child node of type {childNode.GetType().Name}. Expected a PropertyNode representing a named color definition.", childNode.Span, childNode.LinePosition);
                    continue;
                }
                if (TryParseToGameColor(propertyNode, out var gameColor))
                {
                    colors.Add(new NamedColor(propertyNode.Key.Text, gameColor));
                }
            }
            return new LoadOutput<NamedColor>(colors, _diagnostics);
        }

        private bool TryParseToGameColor(PropertyNode node, out GameColor color)
        {
            if (PropertyNodeParsers.TryParseToGameColor(node, out color, out var diagnostic))
            {
                return true;
            }
            else
            {
                _diagnostics.Add(diagnostic with { FilePath = FilePath });
                color = default;
                return false;
            }
        }



        // エラー診断を追加するためのヘルパーメソッド
        private void AddError(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, message, span, linePosition, FilePath));
        private void AddWarning(string message, TextSpan span, LinePosition linePosition)
            => _diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, message, span, linePosition, FilePath));
    }
    }
