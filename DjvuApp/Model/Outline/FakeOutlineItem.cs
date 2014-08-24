using System.Collections.Generic;
using System.Diagnostics;

namespace DjvuApp.Model.Outline
{
    class FakeOutlineItem : IOutlineItem
    {
        public string Title { get; private set; }
        public uint PageNumber { get; private set; }
        public bool HasItems { get { return Items.Count > 0; } }
        public IReadOnlyList<IOutlineItem> Items { get; private set; }
        public IOutlineItem Parent { get; private set; }

        public FakeOutlineItem()
        {
            Title = "OUTLINE";
            var items = new List<FakeOutlineItem>();
            
            for (int i = 0; i < 10; i++)
            {
                var item = new FakeOutlineItem(true) {Title = "Глава 1 : проверка", PageNumber = 25, Parent = this};
                items.Add(item);
                item = new FakeOutlineItem(true)
                {
                    Title = "Глава 2 : вапвапр вапрварвар ывпаываывавыа",
                    PageNumber = 64,
                    Parent = this,
                    Items = items
                };
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
}