using FRELODYSHRD.Dtos.AuthDtos;
using Refit;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IOtpApi
    {
        [Post("/api/otp/send-otp")]
        Task<IApiResponse<SendOtpResponseDto>> SendOtp([Body] SendOtpRequestDto request);

        [Post("/api/otp/verify-otp")]
        Task<IApiResponse<VerifyOtpResponseDto>> VerifyOtp([Body] VerifyOtpRequestDto request);

        [Post("/api/otp/resend-otp")]
        Task<IApiResponse<SendOtpResponseDto>> ResendOtp([Query] string email);
    }
}
