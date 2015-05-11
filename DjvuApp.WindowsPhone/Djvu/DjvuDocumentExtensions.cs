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
            return PickSections(outline.AsEnumerable().GetEnumerator(), (uint) outline.Length, null);
        }

        private static IReadOnlyList<IOutlineSection> PickSections(IEnumerator<DjvuBookmark> enumerator, uint count, IOutlineSection parent)
        {
            var result = new List<IOutlineSection>();

            for (uint i = 0; i < count; i++)
            {
                if (!enumerator.MoveNext())
                    break;

                var bookmark = enumerator.Current;

                var url = bookmark.Url;
                uint pageNumber = 0;
                if (url.StartsWith("#"))
                {
                    uint.TryParse(url.Substring(1), out pageNumber);
                }

                var item = new OutlineSection
                {
                    Title = bookmark.Name,
                    PageNumber = pageNumber > 0 ? (uint?) pageNumber : null,
                    Parent = parent
                };

                item.Items = bookmark.ChildrenCount > 0
                    ? PickSections(enumerator, bookmark.ChildrenCount, item)
                    : new IOutlineSection[0];

                result.Add(item);
            }

            return result;
        }
    }
}
