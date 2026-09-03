using System;

namespace Server.Exceptions
{
    public class ApiException : Exception
    {
        public long StatusCode { get; }

        public ApiException(long statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
