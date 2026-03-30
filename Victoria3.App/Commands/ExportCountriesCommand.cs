using PdxScriptAnalysis;
using System.CommandLine;
using Tomlyn;
using Victoria3.App.Config;
using Victoria3.Formatting;
using Victoria3.GameData;
using Victoria3.Loading;
using Victoria3.Loading.Loaders;
using Victoria3.Localization;

namespace Victoria3.App.Commands
{
    internal class ExportCountriesCommand : Command
    {
        internal ExportCountriesCommand() : base("countries", "ゲーム内の国のデータをCSV形式でエクスポートします")
        {
            this.SetAction(async parseResult =>
            {
                Console.WriteLine("国のデータをCSV形式でエクスポートしています...");
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

                var text = new CsvFormatter<Country>().Format(output.Values, localizer);

                var outputDir = Path.Combine(Environment.CurrentDirectory, config.Output.Directory);
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }
                var outputPath = Path.Combine(outputDir, "countries.csv");
                File.WriteAllText(outputPath, text);

                foreach (var diagnostic in output.Diagnostics)
                {
                    Console.WriteLine($"診断結果: {diagnostic.Message} at {diagnostic.LinePosition}");
                }
            });
        }
    }
}
