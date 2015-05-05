using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using DjvuApp.Djvu;
using DjvuApp.Misc;
using JetBrains.Annotations;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using DeviceContext1 = SharpDX.Direct2D1.DeviceContext1;

namespace DjvuApp.Controls
{
    public static class Renderer
    {
        public static SharpDX.Direct3D11.Device2 D3DDevice { get; set; }

        public static SharpDX.DXGI.Device3 DXGIDevice { get; private set; }

        public static SharpDX.Direct2D1.Factory2 D2DFactory { get; private set; }

        public static SharpDX.Direct2D1.Device1 D2DDevice { get; set; }

        public static SharpDX.Direct2D1.DeviceContext1 D2DDeviceContext { get; set; }

        static Renderer()
        {
            var d3dDevice = new SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);
            D3DDevice = d3dDevice.QueryInterface<SharpDX.Direct3D11.Device2>();
            DXGIDevice = D3DDevice.QueryInterface<SharpDX.DXGI.Device3>();
            D2DFactory = new SharpDX.Direct2D1.Factory2(FactoryType.MultiThreaded, DebugLevel.Information);
            D2DDevice = new SharpDX.Direct2D1.Device1(D2DFactory, DXGIDevice);
            D2DDeviceContext = new DeviceContext1(D2DDevice, DeviceContextOptions.EnableMultithreadedOptimizations);
        }
    }

    public sealed class DjvuPageSource : INotifyPropertyChanged
    {
        public double Width { get; private set; }

        public double Height { get; private set; }

        public uint PageNumber { get; private set; }

        public double ZoomFactor { get; private set; }

        private VirtualSurfaceImageSource _foregroundImageSource = null;

        public VirtualSurfaceImageSource ForegroundImageSource
        {
            get { return _foregroundImageSource; }

            private set
            {
                if (_foregroundImageSource == value)
                {
                    return;
                }

                _foregroundImageSource = value;
                RaisePropertyChanged();
            }
        }

        private VirtualSurfaceImageSource _backgroundImageSource = null;

        public VirtualSurfaceImageSource BackgroundImageSource
        {
            get { return _backgroundImageSource; }

            private set
            {
                if (_backgroundImageSource == value)
                {
                    return;
                }

                _backgroundImageSource = value;
                RaisePropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                _propertyChanged += value;
                _subscriptionCount++;

                if (_subscriptionCount == 1)
                {
                    Load();
                }
            }
            remove
            {
                _propertyChanged -= value;
                _subscriptionCount--;

                if (_subscriptionCount == 0)
                {
                    Unload();
                }
            }
        }

        private Size2 GetZoomedSize()
        {
            var width = (int) (Width * ZoomFactor);
            var height = (int) (Height * ZoomFactor);
            return new Size2(width, height);
        }

        private readonly DjvuDocument _document;
        private DjvuPage _page;

        private async void Load()
        {
            _page = await _document.GetPageAsync(PageNumber);
            var size = GetZoomedSize();
            ForegroundImageSource = new VirtualSurfaceImageSource(size.Width, size.Height, true);
            var vsisNative = ComObject.As<IVirtualSurfaceImageSourceNative>(ForegroundImageSource);
            vsisNative.Device = Renderer.DXGIDevice;
            vsisNative.UpdatesNeeded += UpdatesNeededHandler;
        }

        private void UpdatesNeededHandler(object sender, EventArgs e)
        {
            var vsisNative = (IVirtualSurfaceImageSourceNative) sender;
            var updateRectangles = vsisNative.UpdateRectangles;
            foreach (var updateRect in updateRectangles)
            {
                SharpDX.Point offset;
                var surface = vsisNative.BeginDraw(updateRect, out offset);
                var d2dDeviceContext = Renderer.D2DDeviceContext;
                var rowSize = updateRect.Width * 4;
                var bufferSize = rowSize * updateRect.Height;
                var dataStream = new DataStream(bufferSize, true, true);
                _page.RenderRegion((uint) dataStream.DataPointer, new Size(Width, Height), new Rect(updateRect.Left, Height - (updateRect.Top + updateRect.Height), updateRect.Width, updateRect.Height));
                var renderTargetBitmap = new SharpDX.Direct2D1.Bitmap1(d2dDeviceContext, surface);
                var bitmap = new SharpDX.Direct2D1.Bitmap1(d2dDeviceContext, updateRect.Size, dataStream, rowSize);
                d2dDeviceContext.Target = renderTargetBitmap;
                d2dDeviceContext.Transform = Matrix3x2.Translation(offset.X, offset.Y);
                d2dDeviceContext.BeginDraw();
                d2dDeviceContext.DrawImage(bitmap);
                d2dDeviceContext.EndDraw();
                d2dDeviceContext.Transform = Matrix3x2.Identity;
                d2dDeviceContext.Target = null;
                vsisNative.EndDraw();
            }
        }

        private void Unload()
        {
            ForegroundImageSource = null;
            BackgroundImageSource = null;
            var vsisNative = ComObject.As<IVirtualSurfaceImageSourceNative>(ForegroundImageSource);
            vsisNative.Device = null;
            vsisNative.UpdatesNeeded -= UpdatesNeededHandler;
        }

        private event PropertyChangedEventHandler _propertyChanged;

        private DjvuDocumentSource _documentSource;
        private uint _subscriptionCount;

        public DjvuPageSource(DjvuDocument document, uint pageNumber)
        {
            PageNumber = pageNumber;
            _document = document;
        }

        [NotifyPropertyChangedInvocator]
        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = _propertyChanged;
            if (handler != null) 
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}