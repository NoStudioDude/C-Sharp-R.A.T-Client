using TutClient.Helpers;

using Unity;
using Unity.Extension;
using Unity.Lifetime;

namespace TutClient.Bootstrapper
{
    public class HelpersUnityContainer : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container.RegisterType<IEncryptionHelper, EncryptionHelper>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IReportHelper, ReportHelper>(new ContainerControlledLifetimeManager());
        }
    }
}