using NUnit.Framework;

namespace MiniContainer.Tests
{
    public class DIServiceTests
    {
        [Test]
        public void IsRegistered_ReturnsTrueAfterRegistrationIsProcessed()
        {
            var service = new DIService();
            var container = service.GenerateContainer();
            service.Register<ITestService, TestService>();

            container.ResolveInstanceRegistered();

            Assert.That(service.IsRegistered<ITestService>(), Is.True);
        }

        [Test]
        public void IsRegistered_ReturnsFalseForIgnoredPendingRegistration()
        {
            var service = new DIService();
            service.Register<IIgnoredService, IgnoredService>();
            service.IgnoreType<IIgnoredService>();

            Assert.That(service.IsRegistered<IIgnoredService>(), Is.False);
        }

        [Test]
        public void IsRegistered_ReturnsFalseAfterServiceIsReleased()
        {
            var service = new DIService();
            var container = service.GenerateContainer();
            service.Register<ITestService, TestService>();
            container.ResolveInstanceRegistered();
            container.Resolve(typeof(ITestService));

            container.Release(typeof(ITestService));

            Assert.That(service.IsRegistered<ITestService>(), Is.False);
        }

        private interface ITestService
        {
        }

        private sealed class TestService : ITestService
        {
            public TestService()
            {
            }
        }

        private interface IIgnoredService
        {
        }

        private sealed class IgnoredService : IIgnoredService
        {
        }
    }
}
