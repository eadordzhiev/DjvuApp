using System;
using Windows.ApplicationModel;
using DjvuApp.Common;
using DjvuApp.Model;
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
            container.RegisterInstance<IBookProvider>(EfBookProvider.CreateAsync().GetAwaiter().GetResult());
            container.RegisterInstance<INavigationService>(new NavigationService());
        }
    }
}
