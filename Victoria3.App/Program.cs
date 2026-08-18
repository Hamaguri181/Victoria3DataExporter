using System.CommandLine;
using Victoria3.App.Commands;

namespace Victoria3.App
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Victoria 3 データ解析ツール");

            rootCommand.Subcommands.Add(new InitCommand());
            rootCommand.Subcommands.Add(new ConfigCommand());
            rootCommand.Subcommands.Add(new ListCommand());
            rootCommand.Subcommands.Add(new ExportCommand());


            if (args.Length <= 0)
            {
                string? input = null;
                while (input is null)
                {
                    Console.WriteLine("コマンドを入力してください");
                    input = Console.ReadLine();
                }
                args = input.Split(" ");
            }

            return await rootCommand.Parse(args).InvokeAsync();
        }
    }
}
