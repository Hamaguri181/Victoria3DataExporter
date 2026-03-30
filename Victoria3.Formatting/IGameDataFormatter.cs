using Victoria3.Localization;

namespace Victoria3.Formatting
{
    public interface IGameDataFormatter<T>
    {
        public string Format(IEnumerable<T> items, ILocalizer? localizer = null);
    }
}
