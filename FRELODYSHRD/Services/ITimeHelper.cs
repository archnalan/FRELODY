
public interface ITimeHelper
{
    DateTime ConvertLocalToUtc(DateTime local);
    DateTimeOffset ConvertLocalToUtc(DateTimeOffset localOffset);
    DateTime ConvertUtcToLocal(DateTime utc);
    DateTimeOffset ConvertUtcToLocal(DateTimeOffset utcOffset);
    string FormatUtcToLocal(DateTime utc, string format);
    DateTime GetCurrentLocalTime();
    DateTime GetCurrentUtcTime();
    string GetLocalTimeZoneDisplayName();
    TimeSpan GetLocalUtcOffset(DateTime? forDate = null);
    bool IsAmbiguousLocalTime(DateTime local);
    bool IsInvalidLocalTime(DateTime local);
}