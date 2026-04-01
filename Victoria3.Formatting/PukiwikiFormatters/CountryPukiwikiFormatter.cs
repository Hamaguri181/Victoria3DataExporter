using System.Collections.Frozen;
using System.Text;
using Victoria3.GameData;
using Victoria3.Localization;

namespace Victoria3.Formatting.PukiwikiFormatters
{
    public sealed class CountryPukiwikiFormatter()
    {
        public string Format(
            IEnumerable<Country> items,
            IEnumerable<HistoricalStateRegion> historicalStateRegions,
            IEnumerable<ReleasableCountry> releasableCountries,
            IEnumerable<FormableCountry> formableCountries,
            ILocalizer localizer,
            ILocalizer englishLocalizer)
        {
            var sb = new StringBuilder();

            sb.AppendLine("|||||50|65|||||35|35|35|c");
            sb.AppendLine("||タグ|>|国名|種別|ティア|主要文化|国教((第一主要文化の文化宗教と異なる場合のみ記載))|ヒエラルキー|首都|初期存在|解放可能|形成可能|h");

            var initialTags = historicalStateRegions
                .SelectMany(hsr => hsr.CreateStates)
                .Select(cs => RemovePrefix(cs.Country))
                .ToFrozenSet();
            var releasableTags = releasableCountries.Select(rc => rc.Tag).ToFrozenSet();
            var formableTags = formableCountries.Select(fc => fc.Tag).ToFrozenSet();
            foreach (var country in items)
            {
                var englishName = englishLocalizer.Localize(country.Tag);
                var name = localizer.Localize(country.Tag);
                var countryType = localizer.Localize(country.Type.ToLocalizationKey());
                var tier = localizer.Localize(country.Tier.ToLocalizationKey());
                var cultures = string.Join("&br;", country.Cultures.Select(c => localizer.Localize(c)));
                var religion = localizer.Localize(country.Religion);
                var hierarchy = localizer.Localize(country.SocialHierarchy);
                var capital = localizer.Localize(country.Capital);
                var isInitial = initialTags.Contains(country.Tag) ? "初期" : "";
                var isReleasable = releasableTags.Contains(country.Tag) ? "解放" : "";
                var isFormable = formableTags.Contains(country.Tag) ? "形成" : "";

                sb.AppendLine($"|BGCOLOR({country.Color.ToColorCode()}):|~{country.Tag}|{englishName}|{name}|{countryType}|{tier}|{cultures}|{religion}|{hierarchy}|{capital}|{isInitial}|{isReleasable}|{isFormable}|");
            }
            return sb.ToString();
        }
        private static string RemovePrefix(string key)
        {
            var index = key.IndexOf(':');
            if (index >= 0)
            {
                return key[(index + 1)..];
            }
            return key;
        }
    }
}
