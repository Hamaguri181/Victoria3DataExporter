using System.Text;
using Victoria3.GameData;
using Victoria3.Localization;

namespace Victoria3.Formatting
{
    public class FormableCountryPukiwikiFormatter
    {
        public string Format(
            IEnumerable<FormableCountry> items,
            ILocalizer localizer)
        {
            var sb = new StringBuilder();

            sb.AppendLine("|CENTER:|LEFT:||130|150||||c");
            sb.AppendLine("|~ |~国名|タグ|主要文化|条件|必要州|国家ティア|備考|h");

            foreach (var formableCountry in items)
            {
                var name = localizer.Localize(formableCountry.Tag);

                sb.AppendLine($"||~{name}|{formableCountry.Tag}||||||");
            }
            return sb.ToString();
        }
    }
}
