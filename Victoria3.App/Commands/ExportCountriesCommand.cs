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
    internal class ExportCountriesCommand : Command
    {
        internal ExportCountriesCommand() : base("countries", "ゲーム内の国のデータをエクスポートします")
        {
            var formatOption = new FormatOption();
            var languageOption = new LanguageOption();

            this.Options.Add(formatOption);
            this.Options.Add(languageOption);

            this.SetAction(async parseResult =>
            {
                var format = parseResult.GetValue(formatOption);
                var language = parseResult.GetValue(languageOption);

                Console.WriteLine($"国のデータを{format}形式でエクスポートしています...");

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
                var output = LoadCountries(gameDir);

                var localizationPath = Path.Combine(gameDir, LocalizationPaths.GetPath(language!));
                var localizer = FileLocalizer.FromDirectory(localizationPath);

                if (format == "csv")
                {
                    var text = CsvFormatter<Country>.Format(output.Values, localizer);
                    var outputDir = Path.Combine(Environment.CurrentDirectory, config.Output.Directory);
                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }
                    var outputPath = Path.Combine(outputDir, "countries.csv");
                    File.WriteAllText(outputPath, text);
                }
                else if (format == "pukiwiki")
                {
                    var historicalStateRegionsOutput = ExportHistoricalStateRegionCommand.LoadHistoricalStateRegions(gameDir);
                    var releasableCountriesOutput = ExportReleasableCountriesCommand.LoadReleasableCountries(gameDir);
                    var formableCountriesOutput = ExportFormableCountriesCommand.LoadFormableCountries(gameDir);

                    var englishLocalizationPath = Path.Combine(gameDir, LocalizationPaths.English);
                    var englishLocalizer = FileLocalizer.FromDirectory(englishLocalizationPath);

                    var formatter = new CountryPukiwikiFormatter();
                    var text = formatter.Format(output.Values, historicalStateRegionsOutput.Values, releasableCountriesOutput.Values, formableCountriesOutput.Values, localizer, englishLocalizer);

                    var outputDir = Path.Combine(Environment.CurrentDirectory, config.Output.Directory);
                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }

                    var outputPath = Path.Combine(outputDir, "countries.txt");
                    File.WriteAllText(outputPath, text);
                }
                else
                {
                    Console.WriteLine($"サポートされていないフォーマット: {format}");
                }
            });
        }

        internal static LoadOutput<Country> LoadCountries(string gameDir)
        {

            // 名前付きカラーの読み込み
            var namedColorDataPath = Path.Combine(gameDir, Victoria3Paths.NamedColors);
            var namedColorScriptTrees = Directory.EnumerateFiles(namedColorDataPath, "*.txt")
                .Select(ScriptTree.ParseFile)
                .ToList();
            var namedColorOutput = new NamedColorLoader(namedColorScriptTrees).Load();
            if (namedColorOutput.Diagnostics.Count > 0)
            {
                Console.WriteLine($"名前付きカラーの読み込み中に診断が発生しました。件数: {namedColorOutput.Diagnostics.Count}");
                foreach (var diagnostic in namedColorOutput.Diagnostics)
                {
                    Console.WriteLine(diagnostic);
                }
            }
            // 名前付きカラーの読み込みが完了した後、国データの読み込みを行う

            var countryDataPath = Path.Combine(gameDir, Victoria3Paths.CountryDefinitions);
            // 解析
            var scriptTrees = Directory.EnumerateFiles(countryDataPath, "*.txt").Select(ScriptTree.ParseFile).ToList();
            Console.WriteLine($"ファイル\"{countryDataPath}\"を解析しました。診断結果: {scriptTrees.Sum(st => st.Diagnostics.Count)}件");
            // ロード
            var output = new CountryLoader(scriptTrees, namedColorOutput.Values).Load();
            Console.WriteLine($"読み込んだ国の数: {output.Values.Count}、診断結果: {output.Diagnostics.Count}件");
            foreach (var diagnostic in output.Diagnostics)
            {
                Console.WriteLine($"診断結果: {diagnostic.Message} at {diagnostic.LinePosition}");
            }
            return output;
        }
    }
}
