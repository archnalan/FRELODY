using FRELODYAPP.Dtos.AuthDtos;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.RefitApis
{
    public interface ISmtpSenderApi
    {
        [Post("/api/smtp-sender/send-mail")]
        Task<IApiResponse<bool>> SendMail([Body] EmailDto emailDto);

        [Post("/api/smtp-sender/send-developer-notification-email")]
        Task<IApiResponse<bool>> SendDeveloperNotificationEmail([Query] string message);

        [Post("/api/smtp-sender/send-password-reset-email")]
        Task<IApiResponse<bool>> SendPasswordResetEmail([Query] string userEmail, [Query] string requestorUri);
    }
}
