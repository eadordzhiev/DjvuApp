using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DjvuApp.Model.Books;
using DjvuApp.Model.Outline;

namespace DjvuApp.Djvu
{
    public static class DjvuDocumentExtensions
    {
        [DebuggerDisplay("{Title} at {PageNumber}")]
        private sealed class OutlineSection : IOutlineSection
        {
            public string Title { get; set; }

            public uint? PageNumber { get; set; }

            public bool HasItems { get { return Items.Count > 0; } }

            public IReadOnlyList<IOutlineSection> Items { get; set; }

            public IOutlineSection Parent { get; set; }
        }

        public static IEnumerable<IOutlineSection> GetOutline(this DjvuDocument document)
        {
            var outline = document.GetBookmarks();
            if (outline == null)
                return null;
            return PickSections(outline, null);
        }

        private static IReadOnlyList<IOutlineSection> PickSections(IEnumerable<DjvuBookmark> bookmarks, IOutlineSection parent)
        {
            var result = new List<IOutlineSection>();

            foreach (var bookmark in bookmarks)
            {
                var item = new OutlineSection();
                item.PageNumber = bookmark.PageNumber;
                item.Title = bookmark.Name;
                item.Items = PickSections(bookmark.Items, item);
                item.Parent = parent;

                result.Add(item);
            }

            return result;
        }
    }
}
