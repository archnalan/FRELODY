using Refit;
using FRELODYSHRD.Dtos.ChatDtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IChatsApi
    {
        [Post("/api/chats/create-anonymous-chat-session")]
        Task<IApiResponse<ChatSessionDto>> CreateAnonymousChatSession();

        [Post("/api/chats/send-message")]
        Task<IApiResponse<ChatMessageDto>> SendMessage([Query] string sessionId, [Query] string message, [Query] bool isFromAdmin = false);

        [Get("/api/chats/get-active-sessions")]
        Task<IApiResponse<List<ChatSessionDto>>> GetActiveSessions();

        [Get("/api/chats/get-session-messages")]
        Task<IApiResponse<List<ChatMessageDto>>> GetSessionMessages([Query] string sessionId);

        [Put("/api/chats/close-session")]
        Task<IApiResponse<bool>> CloseSession([Query] string sessionId);
    }
}