using System;
using System.Collections.Generic;
using System.Text;

namespace Documents.Clients.Manager.Exceptions
{
    public class RecipientAlreadyPresentException : ExceptionBase
    {
        public RecipientAlreadyPresentException(string message) 
            : base(System.Net.HttpStatusCode.Conflict, $"Recipient Already Exists: {message}")
        {

        }
    }
}
