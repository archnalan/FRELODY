namespace FRELODYSHRD.Dtos.AuthDtos
{
    public class DeviceSessionDto
    {
        public int Id { get; set; }
        public string? DeviceId { get; set; }
        public string? DeviceName { get; set; }
        public string? IpAddress { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public bool IsCurrentDevice { get; set; }
    }

    public class RevokeOtherSessionsDto
    {
        public string DeviceId { get; set; } = string.Empty;
    }
}
