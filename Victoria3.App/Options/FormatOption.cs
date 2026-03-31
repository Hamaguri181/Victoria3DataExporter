using System.CommandLine;

namespace Victoria3.App.Options
{
    internal class FormatOption : Option<string>
    {
        internal FormatOption() : base("--format", "-f")
        {
            Description = "エクスポートするフォーマット。現在は\"csv\"と\"pukiwiki\"のみサポートされています。";
            DefaultValueFactory = _ => "pukiwiki";
        }
    }
}
