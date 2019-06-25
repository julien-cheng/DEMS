namespace Documents.Store.Exceptions
{
    public class UnauthorizedException : StoreException
    {
        public UnauthorizedException(string type, string privilege) : base($"Unauthorized! type:{type} privilege:{privilege}")
        {
        }
    }
}
