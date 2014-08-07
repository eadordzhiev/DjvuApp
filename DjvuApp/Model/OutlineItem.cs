using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DjvuLibRT;

namespace DjvuApp
{
    public interface IOutlineItem
    {
        string Title { get; }
        uint PageNumber { get; }
        bool HasItems { get; }
        IReadOnlyList<IOutlineItem> Items { get; }
        IOutlineItem Parent { get; }
    }

    class FakeOutlineItem : IOutlineItem
    {
        public string Title { get; set; }
        public uint PageNumber { get; set; }
        public bool HasItems { get { return Items.Count > 0; } }
        public IReadOnlyList<IOutlineItem> Items { get; set; }
        public IOutlineItem Parent { get; set; }

        public FakeOutlineItem()
        {
            Title = "OUTLINE";
            var items = new List<FakeOutlineItem>();
            
            for (int i = 0; i < 10; i++)
            {
                var item = new FakeOutlineItem(true);
                item.Title = "Глава 1 : проверка";
                item.PageNumber = 25;
                item.Parent = this;
                items.Add(item);
                item = new FakeOutlineItem(true);
                item.Title = "Глава 2 : вапвапр вапрварвар ывпаываывавыа";
                item.PageNumber = 64;
                item.Parent = this;
                items.Add(item);
            }

            for (int i = 0; i < 20; i++)
            {
                items[i].Items = items;
            }

            Items = items;
        }

        private FakeOutlineItem(bool b)
        {
            
        }
    }

    [DebuggerDisplay("{Title} at {PageNumber}")]
    public class OutlineItem : IOutlineItem
    {
        public string Title { get; private set; }

        public uint PageNumber { get; private set; }

        public bool HasItems { get { return Items.Count > 0; } }

        public IReadOnlyList<IOutlineItem> Items { get; private set; }

        public IOutlineItem Parent { get; private set; }

        public static IOutlineItem GetOutline(IList<DjvuBookmark> djvuBookmarks)
        {
            var outline = new OutlineItem();
            outline.Title = "Outline";
            outline.Items = GetOutlineImpl(djvuBookmarks, null);
            return outline;
        }

        private static OutlineItem[] GetOutlineImpl(IList<DjvuBookmark> djvuBookmarks, OutlineItem parent)
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
                    var rawChildren = djvuBookmarks.Skip(i + 1).Take((int) djvuBookmark.ChildrenCount).ToArray();
                    i += (int) djvuBookmark.ChildrenCount;
                    item.Items = GetOutlineImpl(rawChildren, item);
                }
                else
                {
                    item.Items = new OutlineItem[0];
                }

                result.Add(item);
            }

            return result.ToArray();
        }
    }
}