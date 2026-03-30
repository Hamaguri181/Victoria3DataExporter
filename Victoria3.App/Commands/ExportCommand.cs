using System.CommandLine;

namespace Victoria3.App.Commands
{
    internal class ExportCommand : Command
    {
        internal ExportCommand() : base("export", "指定したゲームデータをCSV形式でエクスポートします")
        {
            this.Subcommands.Add(new ExportCountriesCommand());
        }
    }
}
