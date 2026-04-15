using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.AuthDtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface IOtpService
    {
        Task<ServiceResult<SendOtpResponseDto>> SendOtp(SendOtpRequestDto request);
        Task<ServiceResult<VerifyOtpResponseDto>> VerifyOtp(VerifyOtpRequestDto request);
        Task<ServiceResult<SendOtpResponseDto>> ResendOtp(string email);
    }
}
