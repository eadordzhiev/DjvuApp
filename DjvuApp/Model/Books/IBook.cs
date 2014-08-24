using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DjvuApp.Model.Books
{
    public interface IBook : INotifyPropertyChanged, IEquatable<IBook>
    {
        Guid Guid { get; }

        string Title { get; set; }

        DateTime LastOpeningTime { get; }

        DateTime CreationTime { get; }

        uint PageCount { get; }

        uint Size { get; }

        string Path { get; }
    }
}