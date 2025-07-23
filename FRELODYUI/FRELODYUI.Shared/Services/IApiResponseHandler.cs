using Refit;

namespace FRELODYUI.Shared.Services
{
    public interface IApiResponseHandler
    {
        T? ExtractContent<T>(IApiResponse<T> response);
        string GetApiErrorMessage<T>(IApiResponse<T> response);
    }
}