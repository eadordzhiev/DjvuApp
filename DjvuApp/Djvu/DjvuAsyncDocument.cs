using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DjvuApp.Model.Books;
using DjvuApp.Model.Outline;
using DjvuLibRT;

namespace DjvuApp.Djvu
{
    public sealed class DjvuAsyncDocument
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

        public uint PageCount { get; private set; }

        private readonly DjvuDocument _document;
        private readonly static SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private DjvuAsyncDocument(DjvuDocument document)
        {
            _document = document;
            PageCount = document.PageCount;
        }

        public static async Task<DjvuAsyncDocument> LoadFileAsync(string path)
        {
            await _semaphore.WaitAsync();

            DjvuDocument document;
            try
            {
                document = await DjvuDocument.LoadAsync(path);
            }
            catch (Exception ex)
            {
                throw new DjvuDocumentException("Cannot open document.", ex);
            }
            finally
            {
                _semaphore.Release();
            }

            if (document.Type != DocumentType.SinglePage && document.Type != DocumentType.Bundled && document.Type != DocumentType.OldBundled)
            {
                throw new DocumentTypeNotSupportedException("Unsupported document type. Only bundled and single page documents are supported.");
            }

            return new DjvuAsyncDocument(document);
        }

        public async Task<DjvuAsyncPage> GetPageAsync(uint pageNumber)
        {
            await _semaphore.WaitAsync();

            DjvuPage page;
            try
            {
                page = await _document.GetPageAsync(pageNumber);
            }
            finally
            {
                _semaphore.Release();
            }
            
            return new DjvuAsyncPage(page, _semaphore);
        }

        public IReadOnlyList<PageInfo> GetPageInfos()
        {
            return _document.GetPageInfos();
        }

        public IEnumerable<IOutlineItem> GetOutline()
        {
            var outline = _document.GetBookmarks();
            if (outline == null)
                return null;
            return GetOutlineItems(outline, null);
        }

        private static IReadOnlyList<IOutlineItem> GetOutlineItems(IList<DjvuBookmark> djvuBookmarks, OutlineItem parent)
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
                    item.Items = GetOutlineItems(rawChildren, item);
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
