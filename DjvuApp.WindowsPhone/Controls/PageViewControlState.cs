using DjvuApp.Djvu;

namespace DjvuApp.Controls
{
    public class PageViewControlState
    {
        public PageViewControlState(DjvuDocument document, uint pageNumber, double width, double height, ZoomFactorObserver zoomFactorObserver)
        {
            Document = document;
            PageNumber = pageNumber;
            Width = width;
            Height = height;
            ZoomFactorObserver = zoomFactorObserver;
        }

        public DjvuDocument Document { get; private set; }
        public uint PageNumber { get; private set; }
        public double Width { get; private set; }
        public double Height { get; private set; }
        public ZoomFactorObserver ZoomFactorObserver { get; private set; }
    }
}