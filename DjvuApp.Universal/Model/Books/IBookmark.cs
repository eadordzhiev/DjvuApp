using System;
using JetBrains.Annotations;

namespace DjvuApp.Model.Books
{
    public interface IBookmark
    {
        [NotNull]
        string Title { get; }

        uint PageNumber { get; }
    }
}