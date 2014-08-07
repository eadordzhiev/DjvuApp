using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235
using Windows.UI.Xaml.Media.Imaging;
using DjvuLibRT;

namespace DjvuApp
{
    public sealed class ZoomablePagePresenter : Control
    {
        public DjvuPage Source { get; set; }

        public string PageTitle
        {
            get { return (string) GetValue(PageTitleProperty); }
            set { SetValue(PageTitleProperty, value); }
        }

        public static readonly DependencyProperty PageTitleProperty =
            DependencyProperty.Register("PageTitle", typeof (string), typeof (ZoomablePagePresenter),
                new PropertyMetadata(null));

        private Image _documentImage;
        private double _currentScale = 0;
        private bool _needRender = false;

        public ZoomablePagePresenter()
        {
            this.DefaultStyleKey = typeof (ZoomablePagePresenter);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _documentImage = (Image) GetTemplateChild("ImageContainer");

            if (_needRender)
            {
                _needRender = false;
                RenderImpl();
            }
        }

        public async Task Render(double scale)
        {
            if (scale > 1)
                scale = 1;

            if (Math.Abs(_currentScale - scale) < 0.0001)
                return;

            _currentScale = scale;

            Debug.WriteLine("ZoomablePagePresenter: Rescale to {0}", _currentScale);

            if (_documentImage == null)
            {
                _needRender = true;
                return;
            }
            else
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, RenderImpl);
            }
        }

        public void Clear()
        {
            if (Source != null)
            {
                Source.Dispose();
                Source = null;
            }

            _currentScale = 0;
            _needRender = false;

            if (_documentImage != null)
            {
                _documentImage.Source = null;
            }
        }

        private void RenderImpl()
        {
            var s = Stopwatch.StartNew();

            var pixelWidth = (int) (Source.Width*_currentScale);
            var pixelHeight = (int) (Source.Height*_currentScale);
            var size = new Size(pixelWidth, pixelHeight);

            var source = new WriteableBitmap(pixelWidth, pixelHeight);

            Source.RenderRegion(source, size, new Rect(new Point(), size));
            
            _documentImage.Source = source;

            s.Stop();
            Debug.WriteLine("RenderImpl(): {0} ms taken", s.ElapsedMilliseconds);
        }
    }
}