using System.Text;
using Victoria3.GameData;
using Victoria3.Localization;

namespace Victoria3.Formatting.PukiwikiFormatters
{
    public class HistoricalStateRegionPukiwikiFormatter
    {
        public string Format(
            IEnumerable<HistoricalStateRegion> items,
            ILocalizer localizer)
        {
            var sb = new StringBuilder();

            sb.AppendLine("|~州地域名|所有者|母国|請求権|h");

            foreach (var stateRegion in items)
            {
                var name = localizer.Localize(stateRegion.Tag);
                var countries = string.Join(", ", stateRegion.CreateStates.Select(cs => localizer.Localize(cs.Country)));
                var homelands = string.Join(", ", stateRegion.Homelands.Select(h => localizer.Localize(h)));
                var claims = string.Join(", ", stateRegion.Claims.Select(c => localizer.Localize(c)));

                sb.AppendLine($"|{name}|{countries}|{homelands}|{claims}|");
            }
            return sb.ToString();
        }
    }
}
