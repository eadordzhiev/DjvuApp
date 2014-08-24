using System;
using JetBrains.Annotations;

namespace DjvuApp.Model.Books
{
    public interface IBookmark
    {
        string Title { get; }
        uint PageNumber { get; }
    }
}