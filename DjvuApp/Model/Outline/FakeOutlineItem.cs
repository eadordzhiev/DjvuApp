using System.Collections.Generic;

namespace DjvuApp.Model.Outline
{
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
}