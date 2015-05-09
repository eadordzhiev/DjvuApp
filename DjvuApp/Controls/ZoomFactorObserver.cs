using System;

namespace DjvuApp.Controls
{
    public class ZoomFactorObserver
    {
        private float _zoomFactor = 1;

        public float ZoomFactor
        {
            get
            {
                return _zoomFactor;
            }
            set
            {
                _zoomFactor = value;
                RaiseZoomFactorChanged();
            }
        }

        public event Action ZoomFactorChanged;

        private void RaiseZoomFactorChanged()
        {
            var handler = ZoomFactorChanged;
            if (handler != null) handler();
        }
    }
}