using System.CommandLine;
using Tomlyn;
using Victoria3.App.Config;

namespace Victoria3.App.Commands
{
    internal class InitCommand : Command
    {
        internal InitCommand() : base("init", "設定の初期化を行います")
        {
            this.SetAction(parseResult =>
            {
                Console.WriteLine("設定を初期化しています...");
                var configPath = Path.Combine(Environment.CurrentDirectory, "vic3tool.toml");
                var config = new AppConfig()
                {
                    Game = new GameConfig(),
                    Output = new OutputConfig()
                };
                var text = TomlSerializer.Serialize(config);
                Console.WriteLine("設定ファイルのパス: " + configPath);
                File.WriteAllText(configPath, text);
                Console.WriteLine("設定ファイルを初期化しました。");
            });
        }
    }
}
