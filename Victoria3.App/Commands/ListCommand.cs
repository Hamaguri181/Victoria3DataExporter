using System.CommandLine;

namespace Victoria3.App.Commands
{
    internal class ListCommand : Command
    {
        internal ListCommand() : base("list", "指定したゲームデータの一覧を表示します")
        {
            this.Subcommands.Add(new ListCountriesCommand());
            this.Subcommands.Add(new ListNamedColorsCommand());
        }
    }
}
