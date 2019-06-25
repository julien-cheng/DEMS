namespace Documents.Store.Exceptions
{
    using System;

    public class StoreException : Exception
    {
        public StoreException(string message)
            : base(message)
        {
            
        }
    }
}
