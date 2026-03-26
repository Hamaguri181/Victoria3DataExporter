using System.CommandLine;
using Tomlyn;
using Victoria3.App.Config;

namespace Victoria3.App.Commands
{
    internal class ConfigSetCommand : Command
    {
        internal ConfigSetCommand() : base("set", "設定項目を変更します")
        {
            var keyArgument = new Argument<string>("key")
            {
                Description = "設定項目のキー"
            };
            var valueArgument = new Argument<string>("value")
            {
                Description = "設定項目の値"
            };
            this.Arguments.Add(keyArgument);
            this.Arguments.Add(valueArgument);
            this.SetAction(parseResult =>
            {
                var key = parseResult.GetValue(keyArgument);
                var value = parseResult.GetValue(valueArgument);

                if (key is null)
                {
                    Console.WriteLine("設定項目のキーが指定されていません。");
                    return;
                }
                if (value is null)
                {
                    Console.WriteLine("設定項目の値が指定されていません。");
                    return;
                }

                Console.WriteLine($"設定項目 '{key}' を '{value}' に変更します...");

                var configPath = Path.Combine(Environment.CurrentDirectory, "vic3tool.toml");

                var config = new AppConfig();
                Console.WriteLine("設定ファイルのパス: " + configPath);
                if (File.Exists(configPath))
                {
                    var configText = File.ReadAllText(configPath);
                    config = TomlSerializer.Deserialize<AppConfig>(configText);
                }
                else
                {
                    Console.WriteLine("設定ファイルが見つかりません。");
                    return;
                }

                if (config is null)
                {
                    Console.WriteLine("設定ファイルの読み込みに失敗しました。");
                    return;
                }

                switch (key)
                {
                    case "game.directory":
                        config.Game.Directory = value;
                        break;
                    case "output.directory":
                        config.Output.Directory = value;
                        break;
                    default:
                        Console.WriteLine($"未知の設定項目: {key}");
                        return;
                }

                var text = TomlSerializer.Serialize(config);
                File.WriteAllText(configPath, text);
                Console.WriteLine("設定ファイルを更新しました。");
            });
        }
    }
}
