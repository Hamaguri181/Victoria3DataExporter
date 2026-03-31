using System.Text;
using Victoria3.GameData;
using Victoria3.Localization;

namespace Victoria3.Formatting.PukiwikiFormatters
{
    public class ReleasableCountryPukiwikiFormatter
    {
        public string Format(
            IEnumerable<ReleasableCountry> items,
            ILocalizer localizer)
        {
            var sb = new StringBuilder();

            sb.AppendLine("|~ |~国名|タグ|h");

            foreach (var country in items)
            {
                var name = localizer.Localize(country.Tag);

                sb.AppendLine($"||~{name}|{country.Tag}|");
            }
            return sb.ToString();
        }
    }
}
