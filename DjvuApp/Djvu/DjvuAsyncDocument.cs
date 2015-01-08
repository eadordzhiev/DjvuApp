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
    public sealed class DjvuAsyncDocument
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

        public uint PageCount { get; private set; }

        private readonly DjvuDocument _document;

        private DjvuAsyncDocument(DjvuDocument document)
        {
            _document = document;
            PageCount = document.PageCount;
        }

        public static async Task<DjvuAsyncDocument> LoadFileAsync(string path)
        {
            DjvuDocument document;
            try
            {
                document = await DjvuDocument.LoadAsync(path);
            }
            catch (Exception ex)
            {
                throw new DjvuDocumentException("Cannot open document.", ex);
            }

            if (document.Type != DocumentType.SinglePage && document.Type != DocumentType.Bundled && document.Type != DocumentType.OldBundled)
            {
                throw new DocumentTypeNotSupportedException("Unsupported document type. Only bundled and single page documents are supported.");
            }

            return new DjvuAsyncDocument(document);
        }

        public async Task<DjvuAsyncPage> GetPageAsync(uint pageNumber)
        {
            var page = await _document.GetPageAsync(pageNumber);
            
            return new DjvuAsyncPage(page);
        }

        public IReadOnlyList<PageInfo> GetPageInfos()
        {
            return _document.GetPageInfos();
        }

        public IEnumerable<IOutlineSection> GetOutline()
        {
            var outline = _document.GetBookmarks();
            if (outline == null)
                return null;
            return PickSections(outline.AsEnumerable().GetEnumerator(), (uint) outline.Length, null);
        }

        private static IReadOnlyList<IOutlineSection> PickSections(IEnumerator<DjvuBookmark> enumerator, uint count, IOutlineSection parent)
        {
            var result = new IOutlineSection[count];

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

                result[i] = item;
            }

            return result;
        }
    }
}
