using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DjvuLibRT;
using JetBrains.Annotations;

namespace DjvuApp.Model.Outline
{
    public sealed class Outline
    {
        [DebuggerDisplay("{Title} at {PageNumber}")]
        private sealed class OutlineItem : IOutlineItem
        {
            public string Title { get; set; }

            public uint PageNumber { get; set; }

            public bool HasItems { get { return Items.Count > 0; } }

            public IReadOnlyList<IOutlineItem> Items { get; set; }

            public IOutlineItem Parent { get; set; }
        }

        [UsedImplicitly]
        public string Title { get; private set; }

        [UsedImplicitly] 
        public IReadOnlyList<IOutlineItem> Items { get; private set; }

        public Outline(IList<DjvuBookmark> djvuBookmarks)
        {
            Title = "Outline";
            Items = GetOutline(djvuBookmarks, null);
        }

        private static IReadOnlyList<IOutlineItem> GetOutline(IList<DjvuBookmark> djvuBookmarks, OutlineItem parent)
        {
            var result = new List<OutlineItem>();

            for (int i = 0; i < djvuBookmarks.Count; i++)
            {
                var djvuBookmark = djvuBookmarks[i];

                var item = new OutlineItem
                {
                    Title = djvuBookmark.Name,
                    PageNumber = Convert.ToUInt32(djvuBookmark.Url.Substring(1)),
                    Parent = parent
                };

                if (djvuBookmark.ChildrenCount > 0)
                {
                    var rawChildren = djvuBookmarks.Skip(i + 1).Take((int)djvuBookmark.ChildrenCount).ToArray();
                    i += (int)djvuBookmark.ChildrenCount;
                    item.Items = GetOutline(rawChildren, item);
                }
                else
                {
                    item.Items = new List<IOutlineItem>();
                }

                result.Add(item);
            }

            return new List<IOutlineItem>(result);
        }
    }
}