using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DjvuApp.Djvu;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace DjvuApp
{
    public class PageViewControlState
    {
        public DjvuDocument Document { get; set; }
        public uint PageNumber { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    public sealed partial class PageViewControl : UserControl
    {
        public static Renderer Renderer;

        public PageViewControlState State
        {
            get { return (PageViewControlState)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }

        public static readonly DependencyProperty StateProperty =
            DependencyProperty.Register("State", typeof(PageViewControlState), typeof(PageViewControl), new PropertyMetadata(null, StateChangedCallback));

        private VsisWrapper _contentVsis;
        private SisWrapper _thumbnailSis;
        private DjvuPage _page;

        private static void StateChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (PageViewControl) d;
            sender.OnStateChanged();
        }

        private void OnStateChanged()
        {
            Cleanup();

            if (State == null)
                return;

            Width = State.Width;
            Height = State.Height;
        }

        private void Cleanup()
        {
            if (_contentVsis != null)
            {
                _contentVsis.Dispose();
            }

            if (_thumbnailSis != null)
            {
                _thumbnailSis.Dispose();
            }

            thumbnailContentCanvas.Background = null;
            contentCanvas.Background = null;
            _contentVsis = null;
            _thumbnailSis = null;
            _page = null;
        }
        
        private void CreateContentSurface()
        {
            Debug.Assert(_contentVsis == null);

            var pageViewSize = new Size(Width, Height);
            _contentVsis = new VsisWrapper(_page, Renderer, pageViewSize);
            _contentVsis.CreateSurface();

            var contentBackgroundBrush = new ImageBrush
            {
                ImageSource = _contentVsis.Source
            };

            contentCanvas.Background = contentBackgroundBrush;
        }

        private void CreateThumbnailSurface()
        {
            Debug.Assert(_thumbnailSis == null);

            var pageViewSize = new Size(Width / 16, Height / 16);
            _thumbnailSis = new SisWrapper(_page, Renderer, pageViewSize);
            _thumbnailSis.CreateSurface();

            var thumbnailBackgroundBrush = new ImageBrush
            {
                ImageSource = _thumbnailSis.Source
            };

            thumbnailContentCanvas.Background = thumbnailBackgroundBrush;
        }

        public PageViewControl()
        {
            this.InitializeComponent();
        }

        public async void OnContainerContentChanging(ContainerContentChangingEventArgs args, TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs> containerContentChangingCallback)
        {
            switch (args.Phase)
            {
                case 0:
                    blankContentCanvas.Opacity = 1;
                    args.RegisterUpdateCallback(containerContentChangingCallback);
                    break;
                case 1:
                    Debug.Assert(_page == null);
                    _page = await State.Document.GetPageAsync(State.PageNumber);

                    CreateThumbnailSurface();

                    blankContentCanvas.Opacity = 0;
                    thumbnailContentCanvas.Opacity = 1;
                    args.RegisterUpdateCallback(containerContentChangingCallback);
                    break;
                case 2:
                    CreateContentSurface();

                    contentCanvas.Opacity = 1;
                    break;
            }
        }
    }
}
