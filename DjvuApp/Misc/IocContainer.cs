using System;
using Windows.ApplicationModel;
using DjvuApp.Model.Books;
using DjvuApp.ViewModel;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;

namespace DjvuApp.Misc
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

            ConfigureContainer(container);
        }

        private static void ConfigureContainer(IUnityContainer container)
        {
            if (DesignMode.DesignModeEnabled)
            {
                container.RegisterInstance<IBookProvider>(new DataContractBookProvider());
            }
            else
            {
                container.RegisterInstance<IBookProvider>(new SqliteBookProvider());
            }
            
            container.RegisterType<MainViewModel>();
            container.RegisterType<ViewerViewModel>();
        }
    }
}
