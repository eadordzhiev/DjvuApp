using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using DjvuLibRT;

namespace DjvuApp.ViewModel
{
    public sealed class DjvuDocumentViewModel : ObservableCollection<DjvuPageViewModel>
    {
        public double MaxWidth { get; private set; }

        public double MaxHeight { get; private set; }

        public DjvuDocumentViewModel(DjvuDocument document, Size? size = null)
        {
            var pageInfos = document.GetPageInfos();

            for (uint i = 0; i < document.PageCount; i++)
            {
                if (size != null)
                {
                    pageInfos[i].Width = (uint) size.Value.Width;
                    pageInfos[i].Height = (uint) size.Value.Height;
                }
                Add(new DjvuPageViewModel(document, i + 1, pageInfos[i]));
            }

            MaxWidth = pageInfos.Max(info => info.Width);
            MaxHeight = pageInfos.Max(info => info.Height);
        }
    }
}
