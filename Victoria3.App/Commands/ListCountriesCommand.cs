using PdxScriptAnalysis;
using System.CommandLine;
using Tomlyn;
using Victoria3.App.Config;
using Victoria3.Loading;
using Victoria3.Loading.Loaders;
using Victoria3.Localization;

namespace Victoria3.App.Commands
{
    internal class ListCountriesCommand : Command
    {
        internal ListCountriesCommand() : base("countries", "ゲーム内の国の一覧を表示します")
        {
            this.SetAction(async parseResult =>
            {
                Console.WriteLine("国の一覧を表示しています...");

                var configPath = Path.Combine(Environment.CurrentDirectory, "vic3tool.toml");

                var config = new AppConfig();
                if (File.Exists(configPath))
                {
                    var configText = File.ReadAllText(configPath);
                    config = TomlSerializer.Deserialize<AppConfig>(configText);
                }
                else
                {
                    Console.WriteLine("設定ファイルが見つかりません。");
                    Console.WriteLine("設定ファイルのパス: " + configPath);
                    return;
                }

                if (config is null)
                {
                    Console.WriteLine("設定ファイルの読み込みに失敗しました。");
                    return;
                }

                var gameDir = config.Game.Directory;

                var countryDataPath = Path.Combine(gameDir, Victoria3Paths.CountryDefinitions);

                var scriptTrees = Directory.EnumerateFiles(countryDataPath, "*.txt").Select(ScriptTree.ParseFile).ToList();

                Console.WriteLine($"ファイル\"{countryDataPath}\"を解析しました。診断結果: {scriptTrees.Sum(st => st.Diagnostics.Count)}件");

                var output = new CountryLoader(scriptTrees).Load();

                Console.WriteLine($"読み込んだ国の数: {output.Values.Count}、診断結果: {output.Diagnostics.Count}件");
                var localizationPath = Path.Combine(gameDir, LocalizationPaths.Japanese);
                var localizer = FileLocalizer.FromDirectory(localizationPath);

                foreach (var (index, country) in output.Values.Index())
                {
                    Console.WriteLine($"{index + 1,-4}: タグ: {country.Tag}, 名前: {localizer.Localize(country.Tag)}, 種別: {country.Type}, ティア: {country.Tier}");
                }

                foreach (var diagnostic in output.Diagnostics)
                {
                    Console.WriteLine($"診断結果: {diagnostic.Message} at {diagnostic.LinePosition}");
                }
            });
        }
    }
}
