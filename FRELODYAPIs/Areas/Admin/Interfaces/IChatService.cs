using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.ChatDtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface IChatService
    {
        Task<ServiceResult<bool>> CloseSessionAsync(string sessionId);
        Task<ServiceResult<ChatSessionDto>> CreateAnonymousChatSessionAsync();
        Task<ServiceResult<List<ChatSessionDto>>> GetActiveSessionsAsync();
        Task<ServiceResult<List<ChatMessageDto>>> GetSessionMessagesAsync(string sessionId);
        Task<ServiceResult<ChatMessageDto>> SendMessageAsync(string sessionId, string message, bool isFromAdmin = false);
    }
}