using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DjvuApp.Model.Books;
using DjvuApp.ViewModel;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;

namespace DjvuApp.Common
{
    public static class IocContainer
    {
        public static void Init()
        {
            if (ServiceLocator.IsLocationProviderSet)
                throw new Exception("IoC container has already been initialized.");

            IUnityContainer container = new UnityContainer();

            var locator = new UnityServiceLocator(container);
            ServiceLocator.SetLocatorProvider(() => locator);

            container.RegisterInstance<IBookProvider>(SqliteBookProvider.CreateNewAsync().Result);

            container.RegisterType<MainViewModel>();
            container.RegisterType<ViewerViewModel>();
        }
    }
}
