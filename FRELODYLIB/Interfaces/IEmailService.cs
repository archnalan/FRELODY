using FRELODYAPP.Dtos.AuthDtos;
using FRELODYLIB.ServiceHandler.ResultModels;

namespace FRELODYLIB.Interfaces
{
    public interface IEmailService
    {
        Task<ServiceResult<bool>> SendEmailAsync(EmailDto emailDto, CancellationToken cancellationToken = default);
    }
}