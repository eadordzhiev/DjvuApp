using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuApp.ViewModel;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;

namespace DjvuApp.Common
{
    public static class IocContainer
    {
        public static void Init()
        {
            if (ServiceLocator.IsLocationProviderSet)
                return;

            var provider = new SimpleIoc();
            ServiceLocator.SetLocatorProvider(() => provider);

            provider.Register<IBookProvider, BookProvider>();

            provider.Register<MainViewModel>();
        }
    }
}
