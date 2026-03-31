using System.Text;
using Victoria3.GameData;
using Victoria3.Localization;

namespace Victoria3.Formatting
{
    public class CountryPukiwikiFormatter()
    {
        public string Format(
            IEnumerable<Country> items,
            IEnumerable<FormableCountry> formableCountries,
            ILocalizer localizer,
            ILocalizer englishLocalizer)
        {
            var sb = new StringBuilder();

            sb.AppendLine("||||50|65|||||35|35|35|c");
            sb.AppendLine("|タグ|>|国名|種別|ティア|主要文化|国教((第一主要文化の文化宗教と異なる場合のみ記載))|ヒエラルキー|首都|初期存在|解放可能|形成可能|h");

            foreach (var country in items)
            {
                var englishName = englishLocalizer.Localize(country.Tag);
                var name = localizer.Localize(country.Tag);
                var countryType = localizer.Localize(country.Type.ToLocalizationKey());
                var tier = localizer.Localize(country.Tier.ToLocalizationKey());
                var cultures = string.Join("&br;", country.Cultures.Select(localizer.Localize));
                var religion = localizer.Localize(country.Religion);
                var hierarchy = localizer.Localize(country.SocialHierarchy);
                var capital = localizer.Localize(country.Capital);
                var isFormable = formableCountries.Any(fc => fc.Tag == country.Tag) ? "形成" : "";

                sb.AppendLine($"|~{country.Tag}|{englishName}|{name}|{countryType}|{tier}|{cultures}|{religion}|{hierarchy}|{capital}|||{isFormable}|");
            }
            return sb.ToString();
        }
    }
}
