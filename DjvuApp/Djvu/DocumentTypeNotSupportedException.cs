using System;

namespace DjvuApp.Djvu
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