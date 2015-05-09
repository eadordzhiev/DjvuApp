using DjvuApp.Djvu;

namespace DjvuApp.Controls
{
    public class PageViewControlState
    {
        public DjvuDocument Document { get; set; }
        public uint PageNumber { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public ZoomFactorObserver ZoomFactorObserver { get; set; }
    }
}