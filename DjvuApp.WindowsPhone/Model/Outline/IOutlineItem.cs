using System.Collections.Generic;
using JetBrains.Annotations;

namespace DjvuApp.Model.Outline
{
    public interface IOutlineSection
    {
        [NotNull]
        string Title { get; }

        uint? PageNumber { get; }

        bool HasItems { get; }

        IReadOnlyList<IOutlineSection> Items { get; }

        IOutlineSection Parent { get; }
    }
}