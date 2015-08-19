using System;
using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;

namespace DjvuApp.Model.Books
{
    public interface IBook : INotifyPropertyChanged, IEquatable<IBook>
    {
        Guid Guid { get; }

        [NotNull]
        string Title { get; }

        DateTime LastOpeningTime { get; }

        uint? LastOpenedPage { get; }

        DateTime CreationTime { get; }

        uint PageCount { get; }

        uint Size { get; }

        [NotNull]
        string Path { get; }

        [NotNull]
        string ThumbnailPath { get; }
    }
}