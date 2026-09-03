namespace Server.Exceptions
{
    public class ValidationException : ApiException
    {
        public ValidationException(string message)
            : base(400, message) { }
    }
}
