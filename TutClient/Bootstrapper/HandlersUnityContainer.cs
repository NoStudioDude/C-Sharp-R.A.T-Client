using TutClient.Handlers;

using Unity;
using Unity.Extension;
using Unity.Lifetime;

namespace TutClient.Bootstrapper
{
    public class HandlersUnityContainer : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container.RegisterType<ICommandHandler, CommandHandler>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IConnectionHandler, ConnectionHandler>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IIpcClientHandler, IpcClientHandler>(new ContainerControlledLifetimeManager());
            Container.RegisterType<ISignalHandler, SignalHandler>(new ContainerControlledLifetimeManager());
        }
    }
}