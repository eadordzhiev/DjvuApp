using System;
using Windows.ApplicationModel;
using DjvuApp.Common;
using DjvuApp.Misc.TrialExperience;
using DjvuApp.Model.Books;
using DjvuApp.ViewModel;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using Microsoft.WindowsAzure.MobileServices;

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
                container.RegisterInstance<IBookProvider>(new DataContractBookProvider(), new ContainerControlledLifetimeManager());
            }
            else
            {
                var provider = CachedSqliteBookProvider.CreateNewAsync().Result;
                container.RegisterInstance<IBookProvider>(provider, new ContainerControlledLifetimeManager());
            }

            container.RegisterInstance<INavigationService>(new NavigationService());

            var azureClient = new MobileServiceClient("https://uselessguyapptrials.azure-mobile.net/", "ErnpeBgAaiBeOZwBzCVmdjlaHSIoPn50");
            container.RegisterInstance<ITrialService>(new AzureTrialService(azureClient));

            container.RegisterType<MainViewModel>();
            container.RegisterType<ViewerViewModel>();
        }
    }
}
