using Windows.UI.Xaml.Navigation;
using JetBrains.Annotations;

namespace DjvuApp.ViewModel.Messages
{
    public class OnNavigatedToMessage<[UsedImplicitly] T>
    {
        public NavigationEventArgs EventArgs { get; }

        public OnNavigatedToMessage(NavigationEventArgs eventArgs)
        {
            EventArgs = eventArgs;
        }
    }
}