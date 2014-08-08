using System.Collections.Generic;

namespace DjvuApp.Model.Outline
{
    public interface IOutlineItem
    {
        string Title { get; }
        uint PageNumber { get; }
        bool HasItems { get; }
        IReadOnlyList<IOutlineItem> Items { get; }
        IOutlineItem Parent { get; }
    }
}