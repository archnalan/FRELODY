using DocumentFormat.OpenXml.Wordprocessing;
using SongsWithChords.Dtos.SubDtos;

namespace SongsWithChords.Dtos.AuthDtos
{
    public class EmailDto : BaseEntityDto
    {
        public string? FromEmail { get; set; }
        public string? ReplyToEmail { get; set; }
        public string? ToEmail { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public string? SenderName { get; set; }
        public string? CompanyName { get; set; }
        public string? emailSenderAccount { get; set; }
        public string? emailSenderSecret { get; set; }
        public bool IsHtml { get; set; }
        public List<string> CcEmails { get; set; } = new List<string>();
        public List<string> BccEmails { get; set; } = new List<string>();
        public List<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();

        public EmailDto(bool isHtml = false)
        {
            IsHtml = isHtml;
        }
    }

    public class EmailAttachment
    {
        public string FileName { get; set; }
        public byte[] Content { get; set; }
        public string ContentType { get; set; }
    }
}
