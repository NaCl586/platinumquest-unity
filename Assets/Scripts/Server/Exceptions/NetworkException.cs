namespace Server.Exceptions
{
    public class NetworkException : ApiException
    {
        public NetworkException(string message)
            : base(0, message) { }
    }
}
