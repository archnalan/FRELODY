using SongsWithChords.Dtos.AuthDtos;

namespace SongsWithChords.Data
{
    public interface IEmailService
    {
        void SendEmail(EmailDto emailDto);
    }
}