using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class ReaderControl : UserControl
    {
        public DjvuDocument Source
        {
            get { return (DjvuDocument)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(DjvuDocument), typeof(ReaderControl), new PropertyMetadata(null, SourceChangedCallback));

        private bool _isLoaded;

        public ReaderControl()
        {
            this.InitializeComponent();

            Loaded += LoadedHandler;
            Unloaded += UnloadedHandler;
            SizeChanged += SizeChangedHandler;
        }

        private void OnSourceChanged()
        {
            if (!_isLoaded)
                return;

            if (Source != null)
            {
                Load();
            }
            else
            {
                Unload();
            }
        }

        private void Load()
        {
            if (Source == null)
                return;

            var pageInfos = Source.GetPageInfos();
            double maxWidth = pageInfos.Max(pageInfo => pageInfo.Width);
            var containerSize = new Size(ActualWidth, ActualHeight);

            var states = new PageViewControlState[Source.PageCount];

            for (uint i = 0; i < states.Length; i++)
            {
                var pageInfo = pageInfos[i];
                var scaleFactor = pageInfo.Width / maxWidth;
                var aspectRatio = ((double) pageInfo.Width) / pageInfo.Height;
                var width = scaleFactor * containerSize.Width;
                var height = width / aspectRatio;
                var state = new PageViewControlState
                {
                    Document = Source,
                    PageNumber = i + 1,
                    Width = width,
                    Height = height
                };
                states[i] = state;
            }

            listView.ItemsSource = states;
        }

        private void Unload()
        {
            listView.ItemsSource = null;
        }

        private void SizeChangedHandler(object sender, SizeChangedEventArgs e)
        {
            Load();
        }

        private void UnloadedHandler(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;
        }

        private void LoadedHandler(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
        }

        private static void SourceChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (ReaderControl) d;
            sender.OnSourceChanged();
        }

        private void ContainerContentChangingHandler(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            var pageViewControl = (PageViewControl) args.ItemContainer.ContentTemplateRoot;
            pageViewControl.OnContainerContentChanging(args, ContainerContentChangingHandler);
        }
    }
}
