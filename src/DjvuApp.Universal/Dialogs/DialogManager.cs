using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.System.Threading;
using Windows.UI.Core;

namespace DjvuApp.Dialogs
{
    public sealed class DialogManager
    {
        private static readonly ThreadLocal<DialogManager> Instance = new ThreadLocal<DialogManager>(() => new DialogManager()); 
        private readonly List<WeakReference<IAsyncInfo>> _pendingDialogs = new List<WeakReference<IAsyncInfo>>();

        private class PendingDialogAwaiter : IDisposable
        {
            private readonly DialogManager _dialogManager;
            private readonly WeakReference<IAsyncInfo> _reference;

            public PendingDialogAwaiter(DialogManager dialogManager, WeakReference<IAsyncInfo> reference)
            {
                _dialogManager = dialogManager;
                _reference = reference;
            }

            public void Dispose()
            {
                _dialogManager._pendingDialogs.Remove(_reference);
            }
        }

        public static DialogManager GetForCurrentThread()
        {
            // Check if the current thread is the UI thread
            var window = CoreWindow.GetForCurrentThread();
            return window == null ? null : Instance.Value;
        }

        public IDisposable AddPendingDialog(IAsyncInfo asyncInfo)
        {
            var weakReference = new WeakReference<IAsyncInfo>(asyncInfo);
            _pendingDialogs.Add(weakReference);

            return new PendingDialogAwaiter(this, weakReference);
        }

        public void DismissPendingDialogs()
        {
            foreach (var weakReference in _pendingDialogs)
            {
                IAsyncInfo asyncInfo;
                if (weakReference.TryGetTarget(out asyncInfo))
                {
                    asyncInfo.Cancel();
                }
            }

            _pendingDialogs.Clear();
        }
    }
}
