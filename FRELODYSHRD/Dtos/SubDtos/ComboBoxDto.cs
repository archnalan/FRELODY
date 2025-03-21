using DocumentFormat.OpenXml.Wordprocessing;

namespace FRELODYAPP.Dtos.SubDtos
{
    public class ComboBoxDto : BaseEntityDto
    {
        public int Id { get; set; }
        public string? ValueText { get; set; }
        public string? IdString { get; set; }
    }
}
