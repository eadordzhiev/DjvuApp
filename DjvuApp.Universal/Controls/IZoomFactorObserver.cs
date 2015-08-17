using System;

namespace DjvuApp.Controls
{
    public interface IZoomFactorObserver
    {
        bool IsZooming { get; }
        float ZoomFactor { get; }
        event Action ZoomFactorChanging;
        event Action ZoomFactorChanged;
    }
}