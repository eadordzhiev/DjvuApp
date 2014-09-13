using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.System;
using DjvuApp.Djvu;

namespace DjvuApp.Controls
{
    public sealed class DjvuDocumentSource : IReadOnlyList<DjvuPageSource>
    {
        public uint MaxWidth { get; private set; }

        private readonly List<DjvuPageSource> _pages = new List<DjvuPageSource>();

        public DjvuDocumentSource(DjvuAsyncDocument document)
        {
            // 1GB devices have a 390MB limit
            const ulong highMemoryUsageLimit = 350 * 1024 * 1024;
            double scaleFactor, previewScaleFactor;

            if (MemoryManager.AppMemoryUsageLimit >= highMemoryUsageLimit)
            {
                scaleFactor = 1 / 2D;
                previewScaleFactor = 1 / 16D;
            }
            else
            {
                scaleFactor = 1 / 4D;
                previewScaleFactor = 1 / 32D;
            }

            var pageInfos = document.GetPageInfos();
            
            for (int i = 0; i < document.PageCount; i++)
            {
                var pageInfo = pageInfos[i];
                var page = new DjvuPageSource(
                    document: document, 
                    pageNumber: pageInfo.PageNumber, 
                    width: pageInfo.Width, 
                    height: pageInfo.Height, 
                    scaleFactor: scaleFactor, 
                    previewScaleFactor: previewScaleFactor);
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
