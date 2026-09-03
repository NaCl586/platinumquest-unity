namespace Server.Exceptions
{
    public class ForbiddenException : ApiException
    {
        public ForbiddenException(string message)
            : base(403, message) { }
    }
}
