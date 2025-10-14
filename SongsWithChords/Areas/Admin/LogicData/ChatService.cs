using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Models;
using FRELODYLIB.Interfaces;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.ChatDtos;
using FRELODYSHRD.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class ChatService : IChatService
    {
        private readonly SongDbContext _context;
        private readonly ITenantProvider _tenantProvider;
        private readonly string _userId;
        private readonly ILogger<ChatService> _logger;

        public ChatService(SongDbContext context, ILogger<ChatService> logger, ITenantProvider tenantProvider)
        {
            _context = context;
            _logger = logger;
            _tenantProvider = tenantProvider;
            _userId = _tenantProvider.GetUserId();
        }

        #region Create Anonymous Chat Session
        public async Task<ServiceResult<ChatSessionDto>> CreateAnonymousChatSessionAsync()
        {
            try
            {
                var session = new ChatSession
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = ChatStatus.Active,
                    StartedAt = DateTime.UtcNow
                };

                await _context.ChatSessions.AddAsync(session);
                await _context.SaveChangesAsync();

                var sessionDto = session.Adapt<ChatSessionDto>();
                return ServiceResult<ChatSessionDto>.Success(sessionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating anonymous chat session");
                return ServiceResult<ChatSessionDto>.Failure(ex);
            }
        }
        #endregion

        #region Send Message
        public async Task<ServiceResult<ChatMessageDto>> SendMessageAsync(string sessionId, string message, bool isFromAdmin = false)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionId))
                    return ServiceResult<ChatMessageDto>.Failure(
                        new BadRequestException("Session ID is required."));

                var session = await _context.ChatSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                if (session == null)
                    return ServiceResult<ChatMessageDto>.Failure(
                        new NotFoundException($"Chat session with ID: {sessionId} not found."));

                var chatMessage = new ChatMessage
                {
                    ChatSessionId = session.Id,
                    Message = message,
                    SenderId = isFromAdmin ? _userId : null,
                    IsFromAdmin = isFromAdmin,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };

                await _context.ChatMessages.AddAsync(chatMessage);
                await _context.SaveChangesAsync();

                var messageDto = chatMessage.Adapt<ChatMessageDto>();
                return ServiceResult<ChatMessageDto>.Success(messageDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message for session: {SessionId}", sessionId);
                return ServiceResult<ChatMessageDto>.Failure(ex);
            }
        }
        #endregion

        #region Get Active Sessions
        public async Task<ServiceResult<List<ChatSessionDto>>> GetActiveSessionsAsync()
        {
            try
            {
                var sessions = await _context.ChatSessions
                    .Include(s => s.Messages.OrderByDescending(m => m.SentAt).Take(1))
                    .Where(s => s.Status == ChatStatus.Active)
                    .OrderByDescending(s => s.StartedAt)
                    .ToListAsync();

                var sessionsDto = sessions.Adapt<List<ChatSessionDto>>();
                return ServiceResult<List<ChatSessionDto>>.Success(sessionsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active chat sessions");
                return ServiceResult<List<ChatSessionDto>>.Failure(ex);
            }
        }
        #endregion

        #region Get Session Messages
        public async Task<ServiceResult<List<ChatMessageDto>>> GetSessionMessagesAsync(string sessionId)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionId))
                    return ServiceResult<List<ChatMessageDto>>.Failure(
                        new BadRequestException("Session ID is required."));

                var session = await _context.ChatSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                if (session == null)
                    return ServiceResult<List<ChatMessageDto>>.Failure(
                        new NotFoundException($"Chat session with ID: {sessionId} not found."));

                var messages = await _context.ChatMessages
                    .Where(m => m.ChatSessionId == session.Id)
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();

                var messagesDto = messages.Adapt<List<ChatMessageDto>>();
                return ServiceResult<List<ChatMessageDto>>.Success(messagesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving messages for session: {SessionId}", sessionId);
                return ServiceResult<List<ChatMessageDto>>.Failure(ex);
            }
        }
        #endregion

        #region Close Session
        public async Task<ServiceResult<bool>> CloseSessionAsync(string sessionId)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionId))
                    return ServiceResult<bool>.Failure(
                        new BadRequestException("Session ID is required."));

                var session = await _context.ChatSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                if (session == null)
                    return ServiceResult<bool>.Failure(
                        new NotFoundException($"Chat session with ID: {sessionId} not found."));

                session.Status = ChatStatus.Closed;
                session.EndedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing session: {SessionId}", sessionId);
                return ServiceResult<bool>.Failure(ex);
            }
        }
        #endregion
    }
}