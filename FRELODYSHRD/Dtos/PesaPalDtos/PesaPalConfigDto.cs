namespace FRELODYSHRD.Dtos.PesaPalDtos
{
    /// <summary>Public PesaPal availability flag for the checkout UI.</summary>
    public class PesaPalConfigDto
    {
        /// <summary>False when no PesaPal credentials are configured — show the option disabled.</summary>
        public bool Enabled { get; set; }
    }
}
