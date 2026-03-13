using PdxScriptAnalysis.Syntax;
using System.Text;

namespace PdxScriptAnalysis.Utilities
{
    /// <summary>
    /// 構文木をツリー形式で出力するためのクラス。
    /// </summary>
    public class SyntaxTreePrinter : SyntaxWalker
    {
        private const string Indent = "    ";
        private const string BranchMiddle = "├───";
        private const string BranchLast = "└───";
        private const string Pipe = "│   ";
        private const string Empty = "    ";

        private readonly StringBuilder _builder = new();
        private int _depth = 0;
        private readonly Stack<bool> _isLastStack = new();


        /// <summary>
        /// ツリー形式で構文木を出力する静的メソッド。
        /// </summary>
        /// <param name="node">出力する構文木のルートノード。</param>
        /// <returns>ツリー形式の文字列。</returns>
        public static string Print(SyntaxNode node)
        {
            var printer = new SyntaxTreePrinter();
            printer.Visit(node);
            return printer._builder.ToString();
        }


        protected internal override void VisitRoot(RootNode node)
        {
            WriteLine(FormatNodeInfo(node));
            WriteChildren(node.ChildNodes());
        }

        protected internal override void VisitScalar(ScalarNode node)
        {
            WriteLine($"{node.GetType().Name} {node.Token.Text} {node.Span}");
        }

        protected internal override void VisitBlock(BlockNode node)
        {
            WriteLine(FormatNodeInfo(node));
            WriteChildren(node.ChildNodes());
        }

        protected internal override void VisitScalarProperty(ScalarPropertyNode node)
        {
            WriteLine(FormatNodeInfo(node));
            _depth++;
            WriteTokenLine("Key", node.Key);
            WriteTokenLine("Operator", node.Operator);
            WriteLastChild(node.Value);
            _depth--;
        }

        protected internal override void VisitBlockProperty(BlockPropertyNode node)
        {
            WriteLine(FormatNodeInfo(node));
            _depth++;
            WriteTokenLine("Key", node.Key);
            WriteTokenLine("Operator", node.Operator);
            WriteLastChild(node.Value);
            _depth--;
        }

        protected internal override void VisitTypedBlockProperty(TypedBlockPropertyNode node)
        {
            WriteLine(FormatNodeInfo(node));
            _depth++;
            WriteTokenLine("Key", node.Key);
            WriteTokenLine("Operator", node.Operator);
            WriteTokenLine("TypeQualifier", node.TypeQualifier);
            WriteLastChild(node.Value);
            _depth--;
        }


        private string BuildPrefix()
        {
            var prefixParts = _isLastStack
                .Reverse()
                .Select((isLast, index) => IsDirectParent(index) ? BuildBranch(isLast) : BuildPipe(isLast));
            return string.Concat(prefixParts);
        }

        private bool IsDirectParent(int depth)
            => depth == _depth - 1;
        private static string BuildBranch(bool isLast)
            => isLast ? BranchLast : BranchMiddle;
        private static string BuildPipe(bool isLast)
            => isLast ? Empty : Pipe;

        private void WriteLine(string content)
        {
            _builder.Append(BuildPrefix());
            _builder.AppendLine(content);
        }

        private void WriteTokenLine(string label, SyntaxToken token)
        {
            _builder.Append(BuildPrefix());
            _builder.Append(BranchMiddle);
            _builder.AppendLine($"{label}: {token.Kind} \"{token.Text}\"");
        }

        private void WriteLastChild(SyntaxNode child)
        {
            _isLastStack.Push(true);
            Visit(child);
            _isLastStack.Pop();
        }

        private void WriteChildren(IEnumerable<SyntaxNode> children)
        {
            var childList = children.ToList();
            _depth++;
            for (int i = 0; i < childList.Count; i++)
            {
                var isLast = (i == childList.Count - 1);
                _isLastStack.Push(isLast);
                Visit(childList[i]);
                _isLastStack.Pop();
            }
            _depth--;
        }

        private static string FormatNodeInfo(SyntaxNode node)
            => $"{node.GetType().Name} {node.Span}";
    }
}
