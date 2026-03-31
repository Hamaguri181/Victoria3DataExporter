using System.CommandLine;

namespace Victoria3.App.Options
{
    internal class LanguageOption : Option<string>
    {
        internal LanguageOption() : base("--language", "-l")
        {
            Description = "エクスポートするローカライズの言語。現在は\"japanese\"と\"english\"のみサポートされています。";
            DefaultValueFactory = _ => "japanese";
        }
    }
}
