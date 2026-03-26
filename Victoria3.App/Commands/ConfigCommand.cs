using System.CommandLine;

namespace Victoria3.App.Commands
{
    internal class ConfigCommand : Command
    {
        internal ConfigCommand() : base("config", "ツールの設定を行います")
        {
            this.Subcommands.Add(new ConfigShowCommand());
            this.Subcommands.Add(new ConfigSetCommand());
        }
    }
}
