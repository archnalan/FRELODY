namespace FRELODYLIB.ServiceHandler.ResultModels
{
    public class ServiceResult<T>
    {
        public T Data { get; set; }
        public Exception Error { get; set; }

        public int StatusCode => GetStatusCode();

        private int GetStatusCode()
        {
            if (Error == null) return 200;
			if (Error is NotFoundException) return 404;
            if (Error is KeyNotFoundException) return 404;
			if (Error is BadRequestException) return 400;
			if (Error is ConflictException) return 409;
            if (Error is UnAuthorizedException) return 401;
            if (Error is ForbiddenException) return 403;
            return 500;
		}

        public bool IsSuccess => Error == null;

        public static ServiceResult<T> Success(T data)
        {
            return new ServiceResult<T> { Data = data };           
        }

        public static ServiceResult<T> Failure(Exception error)
        {
            return new ServiceResult<T> { Error = error };
        }
        
    }
}
