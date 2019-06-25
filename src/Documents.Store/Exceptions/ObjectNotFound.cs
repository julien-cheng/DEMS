namespace Documents.Store.Exceptions
{
    public class ObjectNotFound : StoreException
    {
        public ObjectNotFound() : base("Object not found")
        {
        }
    }
}
