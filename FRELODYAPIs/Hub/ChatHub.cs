using FRELODYAPIs.Areas.Admin.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace FRELODYAPIs.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        public async Task JoinSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            _logger.LogInformation("Connection {ConnectionId} joined session {SessionId}",
                Context.ConnectionId, sessionId);
        }

        public async Task SendMessage(string sessionId, string message, bool isFromAdmin)
        {
            var result = await _chatService.SendMessageAsync(sessionId, message, isFromAdmin);

            if (result.IsSuccess)
            {
                // Broadcast to all clients in this session
                await Clients.Group(sessionId).SendAsync("ReceiveMessage", new
                {
                    result.Data.Message,
                    result.Data.SentAt,
                    result.Data.IsFromAdmin,
                    result.Data.SenderId
                });

                // Notify admin dashboard of new user message
                if (!isFromAdmin)
                {
                    await Clients.Group("AdminGroup").SendAsync("NewUserMessage", sessionId, message);
                }
            }
        }

        public async Task NotifyTyping(string sessionId, bool isTyping)
        {
            await Clients.OthersInGroup(sessionId).SendAsync("UserTyping", isTyping);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation("Connection {ConnectionId} disconnected", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}