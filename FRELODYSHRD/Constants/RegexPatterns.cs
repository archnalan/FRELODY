
using System.Text.RegularExpressions;

namespace FRELODYSHRD.Constants
{
    public static class RegexPatterns
    {
        public static Regex ChordRegex = new Regex(
        @"^([A-G])(#|b|bb|##)?(m|maj|min|sus|aug|dim|add)?(\d+)?(/([A-G])(#|b|bb|##)?)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }
}
