using DjvuApp.ViewModel;
using Microsoft.Practices.ServiceLocation;

namespace DjvuApp.Common
{
    public class ViewModelLocator
    {
        public MainViewModel MainViewModel
        {
            get { return ServiceLocator.Current.GetInstance<MainViewModel>(); }
        }

        public ViewerViewModel ViewerViewModel
        {
            get { return ServiceLocator.Current.GetInstance<ViewerViewModel>(); }
        }
    }
}
