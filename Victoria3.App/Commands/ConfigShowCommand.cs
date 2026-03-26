using System.CommandLine;
using Tomlyn;
using Victoria3.App.Config;

namespace Victoria3.App.Commands
{
    internal class ConfigShowCommand : Command
    {
        internal ConfigShowCommand() : base("show", "現在の設定を表示します")
        {
            this.SetAction(parseResult =>
            {
                Console.WriteLine("現在の設定:");
                var configPath = Path.Combine(Environment.CurrentDirectory, "vic3tool.toml");
                Console.WriteLine("設定ファイルのパス: " + configPath);
                if (File.Exists(configPath))
                {
                    var configText = File.ReadAllText(configPath);
                    var config = TomlSerializer.Deserialize<AppConfig>(configText);
                    Console.WriteLine($"ゲームディレクトリ: {config?.Game?.Directory ?? "未設定"}");
                    Console.WriteLine($"出力ディレクトリ: {config?.Output?.Directory ?? "未設定"}");
                }
                else
                {
                    Console.WriteLine("設定ファイルが見つかりません。");
                }
            });
        }
    }
}
