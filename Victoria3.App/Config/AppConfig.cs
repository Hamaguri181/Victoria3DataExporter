namespace Victoria3.App.Config
{
    public sealed class AppConfig
    {
        public GameConfig Game { get; set; } = new();
        public OutputConfig Output { get; set; } = new();
    }
}
