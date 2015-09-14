using System;

namespace DjvuApp.Controls
{
    public sealed class PageViewObserver
    {
        public bool IsZooming { get; private set; }

        public float ZoomFactor { get; private set; }

        public bool IsSelected { get; set; }

        public SelectionMarker SelectionStart { get; set; }

        public SelectionMarker SelectionEnd { get; set; }

        public string SearchText { get; set; }

        public event Action ZoomFactorChanging;

        public event Action ZoomFactorChanged;

        public event Action SelectionChanging;

        public event EventHandler SearchHighlightingRedrawingRequested;

        public PageViewObserver()
        {
            ZoomFactor = 1;
        }

        public void OnZoomFactorChanged(float zoomFactor, bool isIntermediate)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (zoomFactor != ZoomFactor && !IsZooming)
            {
                RaiseZoomFactorChanging();
                IsZooming = true;
            }

            if (!isIntermediate && IsZooming)
            {
                ZoomFactor = zoomFactor;
                RaiseZoomFactorChanged();
                IsZooming = false;
            }
        }

        public void RaiseSelectionChanging()
        {
            SelectionChanging?.Invoke();
        }


        private void RaiseZoomFactorChanged()
        {
            ZoomFactorChanged?.Invoke();
        }

        private void RaiseZoomFactorChanging()
        {
            ZoomFactorChanging?.Invoke();
        }

        public void RaiseSearchHighlightingRedrawingRequested()
        {
            SearchHighlightingRedrawingRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}