using PdxScriptAnalysis;
using System.CommandLine;
using Tomlyn;
using Victoria3.App.Config;
using Victoria3.App.Options;
using Victoria3.Formatting;
using Victoria3.Formatting.PukiwikiFormatters;
using Victoria3.GameData;
using Victoria3.Loading;
using Victoria3.Loading.Loaders;
using Victoria3.Localization;

namespace Victoria3.App.Commands
{
    internal class ExportHistoricalStateRegionCommand : Command
    {
        internal ExportHistoricalStateRegionCommand() : base("historical-state-region", "ゲーム内の歴史的州地域のデータをエクスポートします")
        {
            var formatOption = new FormatOption();
            var languageOption = new LanguageOption();

            this.Options.Add(formatOption);
            this.Options.Add(languageOption);

            this.SetAction(async parseResult =>
            {
                var format = parseResult.GetValue(formatOption);
                var language = parseResult.GetValue(languageOption);

                Console.WriteLine($"解放可能国家のデータを{format}形式でエクスポートしています...");

                // 設定ファイルの読み込み
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
                var output = LoadHistoricalStateRegions(gameDir);

                var localizationPath = Path.Combine(gameDir, LocalizationPaths.GetPath(language!));
                var localizer = FileLocalizer.FromDirectory(localizationPath);

                if (format == "csv")
                {
                    var text = CsvFormatter<HistoricalStateRegion>.Format(output.Values, localizer);
                    var outputDir = Path.Combine(Environment.CurrentDirectory, config.Output.Directory);
                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }
                    var outputPath = Path.Combine(outputDir, "historical_state_regions.csv");
                    File.WriteAllText(outputPath, text);
                }
                else if (format == "pukiwiki")
                {
                    var formatter = new HistoricalStateRegionPukiwikiFormatter();
                    var text = formatter.Format(output.Values, localizer);

                    var outputDir = Path.Combine(Environment.CurrentDirectory, config.Output.Directory);
                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }

                    var outputPath = Path.Combine(outputDir, "historical_state_regions.txt");
                    File.WriteAllText(outputPath, text);
                }
                else
                {
                    Console.WriteLine($"サポートされていないフォーマット: {format}");
                }
            });
        }

        internal static LoadOutput<HistoricalStateRegion> LoadHistoricalStateRegions(string gameDir)
        {
            var countryDataPath = Path.Combine(gameDir, Victoria3Paths.HistoricalStates);
            // 解析
            var scriptTrees = Directory.EnumerateFiles(countryDataPath, "*.txt").Select(ScriptTree.ParseFile).ToList();
            Console.WriteLine($"ファイル\"{countryDataPath}\"を解析しました。診断結果: {scriptTrees.Sum(st => st.Diagnostics.Count)}件");
            // ロード
            var output = new HistoricalStateRegionLoader(scriptTrees).Load();
            Console.WriteLine($"読み込んだ歴史的州地域の数: {output.Values.Count}、診断結果: {output.Diagnostics.Count}件");
            foreach (var diagnostic in output.Diagnostics)
            {
                Console.WriteLine($"診断結果: {diagnostic.Message} at {diagnostic.LinePosition}");
            }
            return output;
        }
    }
}
