using PdxScriptAnalysis;
using System.CommandLine;
using Tomlyn;
using Victoria3.App.Config;
using Victoria3.Loading;
using Victoria3.Loading.Loaders;

namespace Victoria3.App.Commands
{
    /// <summary>
    /// ゲーム内の名前付きカラーの一覧を表示するコマンド。
    /// </summary>
    internal class ListNamedColorsCommand : Command
    {
        internal ListNamedColorsCommand() : base("named-colors", "ゲーム内の名前付きカラーの一覧を表示します")
        {
            this.SetAction(async parseResult =>
            {
                Console.WriteLine("名前付きカラーの一覧を表示しています...");
                var configPath = Path.Combine(Environment.CurrentDirectory, "vic3tool.toml");
                if (!File.Exists(configPath))
                {
                    Console.WriteLine("設定ファイルが見つかりません。");
                    Console.WriteLine("設定ファイルのパス: " + configPath);
                    return;
                }
                var config = new AppConfig();
                var configText = File.ReadAllText(configPath);
                config = TomlSerializer.Deserialize<AppConfig>(configText);
                if (config is null)
                {
                    Console.WriteLine("設定ファイルの読み込みに失敗しました。");
                    return;
                }
                var gameDir = config.Game.Directory;
                var namedColorDataPath = Path.Combine(gameDir, Victoria3Paths.NamedColors);
                var scriptTrees = Directory.EnumerateFiles(namedColorDataPath, "*.txt")
                    .Select(ScriptTree.ParseFile)
                    .ToList();
                Console.WriteLine($"ディレクトリ\"{namedColorDataPath}\"のファイルを解析しました。\n診断件数: {scriptTrees.Sum(st => st.Diagnostics.Count)}件");
                var output = new NamedColorLoader(scriptTrees).Load();
                Console.WriteLine($"{output.Values.Count}個の名前付きカラーを読み込みました。\n診断件数: {output.Diagnostics.Count}件");
                foreach (var (index, namedColor) in output.Values.Index())
                {
                    Console.WriteLine($"{index + 1,-4}: 名前: {namedColor.Name, -25}, RGB: ({namedColor.Color.R, 3}, {namedColor.Color.G, 3}, {namedColor.Color.B, 3})");
                }
                if (output.Diagnostics.Count == 0)
                {
                    Console.WriteLine("診断結果はありません。");
                }
                else
                {
                    Console.WriteLine("診断結果:");
                    foreach (var (index, diagnostic) in output.Diagnostics.Index())
                    {
                        Console.WriteLine($"{index + 1,-4}: {diagnostic}");
                    }
                }
            });
        }
    }
}
