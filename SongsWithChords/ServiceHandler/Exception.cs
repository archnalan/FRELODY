using System.Net;

namespace SongsWithChords.ServiceHandler
{
	public class NotFoundException: Exception
    {
        public NotFoundException(string message):base(message)
        {
        }
    }
    public class BadRequestException: Exception
    {
        public BadRequestException(string message):base(message)
        {
        }
    }
    public class UnAuthorizedException : Exception
    {
        public UnAuthorizedException(string message) : base(message)
        {
        }
    }
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message) : base(message)
        {
        }
    }
    public class ConflictException : Exception
    {
        public ConflictException(string message):base (message)
		{            
        }
    }
    public class ServerErrorException : Exception
    {
        public ServerErrorException(string message) : base(message)
        {
        }
    }
    public class TooManyRequestsException : ApplicationException
    {
        public TooManyRequestsException(string message) : base(message)
        {
            StatusCode = (int)HttpStatusCode.TooManyRequests;
        }

        public int StatusCode { get; }
    }
}
