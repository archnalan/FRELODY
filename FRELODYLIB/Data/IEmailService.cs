using FRELODYAPP.Dtos.AuthDtos;

namespace FRELODYAPP.Data
{
    public interface IEmailService
    {
        void SendEmail(EmailDto emailDto);
    }
}