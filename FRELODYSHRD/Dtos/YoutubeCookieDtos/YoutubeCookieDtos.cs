namespace FRELODYSHRD.Dtos.YoutubeCookieDtos
{
    public class SaveCookiesRequestDto
    {
        public string CookieText { get; set; } = string.Empty;
        public string? Label { get; set; }
    }

    public class SaveCookiesResultDto
    {
        public string SlotName { get; set; } = string.Empty;
        public int CookieCount { get; set; }
        public bool HasAuthCookies { get; set; }
        public List<string> AuthCookiesFound { get; set; } = new();
        public int SlotCount { get; set; }
    }

    public class CookieSlotDto
    {
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset? UpdatedAt { get; set; }
        public bool HasAuthCookies { get; set; }
        public double? ExpiresInDays { get; set; }
    }

    public class CookieStatusDto
    {
        public bool Authenticated { get; set; }
        public int? CookieCount { get; set; }
        public bool? WroteOutput { get; set; }
        public string? ActiveSlot { get; set; }
        public double? ExpiresInDays { get; set; }
        public long? MinExpiryEpoch { get; set; }
        public List<CookieSlotDto> Slots { get; set; } = new();
        public string? Note { get; set; }
        public DateTimeOffset? LastRun { get; set; }
        public double? IntervalHours { get; set; }
        /// <summary>False when cookie-status.json has not been written yet (refresher hasn't cycled).</summary>
        public bool Published { get; set; }
    }
}
