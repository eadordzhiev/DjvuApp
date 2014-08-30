using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DjvuApp.Djvu;

namespace DjvuApp.Controls
{
    public sealed class DjvuDocumentSource : IReadOnlyList<DjvuPageSource>
    {
        public uint MaxWidth { get; private set; }

        private readonly List<DjvuPageSource> _pages = new List<DjvuPageSource>();

        public DjvuDocumentSource(DjvuAsyncDocument document)
        {
            var pageInfos = document.GetPageInfos();

            for (int i = 0; i < document.PageCount; i++)
            {
                var pageInfo = pageInfos[i];
                var page = new DjvuPageSource(
                    document: document, 
                    pageNumber: pageInfo.PageNumber, 
                    width: pageInfo.Width, 
                    height: pageInfo.Height);
                _pages.Add(page);
            }

            MaxWidth = pageInfos.Max(info => info.Width);
        }

        public IEnumerator<DjvuPageSource> GetEnumerator()
        {
            return _pages.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count { get { return _pages.Count; } }

        public DjvuPageSource this[int index]
        {
            get { return _pages[index]; }
        }
    }
}
