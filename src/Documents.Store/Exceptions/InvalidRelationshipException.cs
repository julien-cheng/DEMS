namespace Documents.Store.Exceptions
{
    public class InvalidRelationshipException : StoreException
    {
        public InvalidRelationshipException() : base("Invalid Relationship")
        {
        }
    }
}
