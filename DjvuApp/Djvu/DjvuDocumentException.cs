using System;

namespace DjvuApp.Model.Books
{
    public class DjvuDocumentException : Exception
    {
        public DjvuDocumentException()
        {
        }

        public DjvuDocumentException(string message) : base(message)
        {
        }

        public DjvuDocumentException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}