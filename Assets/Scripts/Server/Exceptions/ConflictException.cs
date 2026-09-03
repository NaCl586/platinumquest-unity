namespace Server.Exceptions
{
    public class ConflictException : ApiException
    {
        public ConflictException(string message)
            : base(409, message) { }
    }
}
