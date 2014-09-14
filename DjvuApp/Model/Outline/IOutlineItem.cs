using System.Collections.Generic;
using JetBrains.Annotations;

namespace DjvuApp.Model.Outline
{
    public interface IOutlineItem
    {
        [NotNull]
        string Title { get; }

        uint? PageNumber { get; }

        bool HasItems { get; }

        IReadOnlyList<IOutlineItem> Items { get; }

        IOutlineItem Parent { get; }
    }
}