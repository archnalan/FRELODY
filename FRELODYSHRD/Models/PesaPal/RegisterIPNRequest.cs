namespace FRELODYSHRD.Models.PesaPal
{

    public class RegisterIPNRequest
    {
        public string IpnUrl { get; set; } = string.Empty;
        public string? NotificationType { get; set; }
    }
}
