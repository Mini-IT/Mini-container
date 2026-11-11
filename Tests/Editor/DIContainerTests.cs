using System;
using NUnit.Framework;

namespace MiniContainer.Tests
{
    public class DIContainerTests
    {
        [TestCase(typeof(IFirstService))]
        [TestCase(typeof(ISecondService))]
        [TestCase(typeof(ScopedService))]
        public void ReleaseScope_PreservesSharedInstanceAcrossInterfacesInFollowingScopes(Type firstServiceType)
        {
            var service = new DIService();
            service.RegisterScoped<ScopedService>().AsImplementedInterfaces();
            var container = service.GenerateContainer();
            ScopedService previousInstance = null;

            try
            {
                for (var i = 0; i < 3; i++)
                {
                    var scope = container.CreateScope();
                    var instance = (ScopedService)container.Resolve(firstServiceType);

                    Assert.AreSame(instance, container.Resolve<IFirstService>());
                    Assert.AreSame(instance, container.Resolve<ISecondService>());
                    Assert.AreSame(instance, container.Resolve<ScopedService>());
                    Assert.AreNotSame(previousInstance, instance);

                    container.ReleaseScope(scope);

                    Assert.AreEqual(1, instance.DisposeCount);
                    previousInstance = instance;
                }
            }
            finally
            {
                container.ReleaseAll();
            }
        }

        [Test]
        public void ReleaseScope_PreservesFactoryRegistrationAndOtherLiveScopes()
        {
            var service = new DIService();
            var registration = service.RegisterScoped(() => new ScopedService())
                .As<IFirstService, ISecondService>();
            var container = service.GenerateContainer();

            try
            {
                var firstScope = container.CreateScope();
                var firstInstance = (ScopedService)container.Resolve<IFirstService>();
                var secondScope = container.CreateScope();
                var secondInstance = (ScopedService)container.Resolve<ISecondService>();

                container.SetCurrentScope(firstScope);
                Assert.AreSame(firstInstance, container.Resolve<ISecondService>());
                container.SetCurrentScope(secondScope);
                Assert.AreSame(secondInstance, container.Resolve<IFirstService>());
                Assert.AreNotSame(firstInstance, secondInstance);

                container.ReleaseScope(firstScope);

                Assert.AreEqual(1, firstInstance.DisposeCount);
                Assert.AreEqual(0, secondInstance.DisposeCount);
                CollectionAssert.AreEquivalent(
                    new[] { typeof(ScopedService), typeof(IFirstService), typeof(ISecondService) },
                    registration.InterfaceTypes);

                container.CreateScope();
                var thirdInstance = (ScopedService)container.Resolve<IFirstService>();

                Assert.AreSame(thirdInstance, container.Resolve<ISecondService>());
                Assert.AreNotSame(firstInstance, thirdInstance);
                Assert.AreNotSame(secondInstance, thirdInstance);

                container.SetCurrentScope(secondScope);
                Assert.AreSame(secondInstance, container.Resolve<IFirstService>());
                Assert.AreSame(secondInstance, container.Resolve<ISecondService>());
            }
            finally
            {
                container.ReleaseAll();
            }
        }

        private interface IFirstService
        {
        }

        private interface ISecondService
        {
        }

        private sealed class ScopedService : IFirstService, ISecondService, IDisposable
        {
            public int DisposeCount { get; private set; }

            public ScopedService()
            {
            }

            public void Dispose()
            {
                DisposeCount++;
            }
        }
    }
}
