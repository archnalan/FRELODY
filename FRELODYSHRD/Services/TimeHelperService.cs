using System;
#if IOS
using Foundation;
#endif

public class TimeHelper : ITimeHelper
{
    private readonly TimeZoneInfo _localTimeZone;

    public TimeHelper()
    {
        _localTimeZone = GetLocalTimeZone();
    }

    /// <summary>
    /// Retrieves the local time zone, handling platform-specific issues.
    /// </summary>
    /// <returns>The local TimeZoneInfo.</returns>
    private TimeZoneInfo GetLocalTimeZone()
    {
        TimeZoneInfo local = TimeZoneInfo.Local;
#if IOS
                try
                {
                    string iosTzName = NSTimeZone.LocalTimeZone.Name;
                    local = TimeZoneInfo.FindSystemTimeZoneById(iosTzName);
                }
                catch (Exception ex)
                {
                    // Fallback to TimeZoneInfo.Local (which may be UTC due to bug); log error in production
                    Console.WriteLine($"iOS time zone lookup failed: {ex.Message}");
                }
#endif

        return local;
    }

    /// <summary>
    /// Converts a UTC DateTime to the local time zone.
    /// </summary>
    /// <param name="utc">The UTC DateTime (must be DateTimeKind.Utc).</param>
    /// <returns>The converted local DateTime.</returns>
    /// <exception cref="ArgumentException">Thrown if input is not UTC.</exception>
    public DateTime ConvertUtcToLocal(DateTime utc)
    {
        if (utc.Kind == DateTimeKind.Unspecified)
        {
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        }
        if (utc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Input DateTime must be UTC or Unspecified (assumed UTC).", nameof(utc));
        }
        return TimeZoneInfo.ConvertTimeFromUtc(utc, _localTimeZone);
    }

    /// <summary>
    /// Converts a local DateTime to UTC.
    /// </summary>
    /// <param name="local">The local DateTime (assumes DateTimeKind.Local or Unspecified).</param>
    /// <returns>The converted UTC DateTime.</returns>
    public DateTime ConvertLocalToUtc(DateTime local)
    {
        if (local.Kind == DateTimeKind.Utc)
        {
            return local; // Already UTC
        }
        return TimeZoneInfo.ConvertTimeToUtc(local.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(local, DateTimeKind.Local) : local, _localTimeZone);
    }

    /// <summary>
    /// Formats a UTC DateTime to a string in local time using the specified format.
    /// </summary>
    /// <param name="utc">The UTC DateTime.</param>
    /// <param name="format">The format string (e.g., "MMM dd:hh tt").</param>
    /// <returns>The formatted local time string.</returns>
    public string FormatUtcToLocal(DateTime utc, string format)
    {
        DateTime local = ConvertUtcToLocal(utc);
        return local.ToString(format);
    }

    /// <summary>
    /// Converts a UTC DateTimeOffset to the local offset.
    /// </summary>
    /// <param name="utcOffset">The UTC DateTimeOffset.</param>
    /// <returns>The local DateTimeOffset.</returns>
    public DateTimeOffset ConvertUtcToLocal(DateTimeOffset utcOffset)
    {
        if (utcOffset.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Input DateTimeOffset must be UTC (offset zero).", nameof(utcOffset));
        }
        TimeSpan localOffset = _localTimeZone.GetUtcOffset(utcOffset.DateTime);
        return utcOffset.ToOffset(localOffset);
    }

    /// <summary>
    /// Converts a local DateTimeOffset to UTC.
    /// </summary>
    /// <param name="localOffset">The local DateTimeOffset.</param>
    /// <returns>The UTC DateTimeOffset.</returns>
    public DateTimeOffset ConvertLocalToUtc(DateTimeOffset localOffset)
    {
        return localOffset.ToUniversalTime();
    }

    /// <summary>
    /// Gets the current local time.
    /// </summary>
    /// <returns>The current DateTime in local time.</returns>
    public DateTime GetCurrentLocalTime()
    {
        return DateTime.Now; // Relies on system local time
    }

    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    /// <returns>The current DateTime in UTC.</returns>
    public DateTime GetCurrentUtcTime()
    {
        return DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the current UTC offset for the local time zone.
    /// </summary>
    /// <param name="forDate">Optional date to calculate offset (defaults to now).</param>
    /// <returns>The TimeSpan offset from UTC.</returns>
    public TimeSpan GetLocalUtcOffset(DateTime? forDate = null)
    {
        DateTime date = forDate ?? DateTime.UtcNow;
        return _localTimeZone.GetUtcOffset(date);
    }

    /// <summary>
    /// Checks if a local time is ambiguous due to DST.
    /// </summary>
    /// <param name="local">The local DateTime.</param>
    /// <returns>True if ambiguous.</returns>
    public bool IsAmbiguousLocalTime(DateTime local)
    {
        return _localTimeZone.IsAmbiguousTime(local);
    }

    /// <summary>
    /// Checks if a local time is invalid due to DST.
    /// </summary>
    /// <param name="local">The local DateTime.</param>
    /// <returns>True if invalid.</returns>
    public bool IsInvalidLocalTime(DateTime local)
    {
        return _localTimeZone.IsInvalidTime(local);
    }

    /// <summary>
    /// Gets the display name of the local time zone.
    /// </summary>
    /// <returns>The time zone display name.</returns>
    public string GetLocalTimeZoneDisplayName()
    {
        return _localTimeZone.DisplayName;
    }
}