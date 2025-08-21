using FRELODYLIB.ServiceHandler.ResultModels;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace SongsWithChords.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            
            _logger.LogError(exception,
                "Exception occurred: {Message} | TraceId: {TraceId}",
                exception.Message,
                traceId);

            var (statusCode, title) = GetErrorResponse(exception);

            var problemDetails = new
            {
                title,
                status = statusCode,
                detail = exception.Message,
                traceId,
                timestamp = DateTime.UtcNow
            };

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/json";

            var response = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await httpContext.Response.WriteAsync(response, cancellationToken);

            return true;
        }

        private static (int StatusCode, string Title) GetErrorResponse(Exception exception)
        {
            return exception switch
            {
                BadRequestException => ((int)HttpStatusCode.BadRequest, "Bad Request"),
                NotFoundException => ((int)HttpStatusCode.NotFound, "Not Found"),
                UnAuthorizedException => ((int)HttpStatusCode.Unauthorized, "Unauthorized"),
                ForbiddenException => ((int)HttpStatusCode.Forbidden, "Forbidden"),
                ConflictException => ((int)HttpStatusCode.Conflict, "Conflict"),
                TooManyRequestsException => ((int)HttpStatusCode.TooManyRequests, "Too Many Requests"),
                ServerErrorException => ((int)HttpStatusCode.InternalServerError, "Internal Server Error"),
                TaskCanceledException => ((int)HttpStatusCode.RequestTimeout, "Request Timeout"),
                ArgumentException => ((int)HttpStatusCode.BadRequest, "Bad Request"),
                //ArgumentNullException => ((int)HttpStatusCode.BadRequest, "Bad Request"),
                InvalidOperationException => ((int)HttpStatusCode.BadRequest, "Bad Request"),
                _ => ((int)HttpStatusCode.InternalServerError, "Internal Server Error")
            };
        }
    }
}