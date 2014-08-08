using System;

namespace DjvuApp.Model.Books
{
    public class DocumentTypeNotSupportedException : Exception
    {
        public DocumentTypeNotSupportedException()
        {
        }

        public DocumentTypeNotSupportedException(string message) : base(message)
        {
        }

        public DocumentTypeNotSupportedException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}