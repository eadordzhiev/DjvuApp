using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;
using JetBrains.Annotations;

namespace DjvuApp.ViewModel.Messages
{
    public sealed class OnNavigatedFromMessage<[UsedImplicitly] T>
    {
        public NavigationEventArgs EventArgs { get; }

        public OnNavigatedFromMessage(NavigationEventArgs eventArgs)
        {
            EventArgs = eventArgs;
        }
    }
}
