using FRELODYAPP.Dtos.AuthDtos;
using FRELODYLIB.ServiceHandler.ResultModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYLIB.Interfaces
{
    public interface ISmtpSenderService
    {
        Task<ServiceResult<bool>> SendDeveloperNotificationEmailAsync(string message);
        Task<ServiceResult<bool>> SendMailAsync(EmailDto emailDto);
        Task<ServiceResult<bool>> SendPasswordResetEmailAsync(string userEmail, string requestorUri);
    }
}
