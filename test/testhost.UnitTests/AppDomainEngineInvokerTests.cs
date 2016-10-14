namespace testhost.UnitTests
{
#if NET46
    using Microsoft.VisualStudio.TestPlatform.TestHost;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [TestClass]
    public class AppDomainEngineInvokerTests
    {
        [TestMethod]
        public void AppDomainEngineInvokerShouldCreateNewAppDomain()
        {
            var tempFile = Path.GetTempFileName();
            var appDomainInvoker = new TestableEngineInvoker(tempFile);

            Assert.IsNotNull(appDomainInvoker.NewAppDomain, "New AppDomain must be created.");
            Assert.IsNotNull(appDomainInvoker.ActualInvoker, "Invoker must be created.");
            Assert.AreNotEqual(AppDomain.CurrentDomain.FriendlyName, appDomainInvoker.NewAppDomain.FriendlyName, 
                "New AppDomain must be different from default one.");
        }

        [TestMethod]
        public void AppDomainEngineInvokerShouldInvokeEngineInNewDomain()
        {
            var tempFile = Path.GetTempFileName();
            var appDomainInvoker = new TestableEngineInvoker(tempFile);

            Assert.IsNotNull(appDomainInvoker.NewAppDomain, "New AppDomain must be created.");
            Assert.IsNotNull(appDomainInvoker.ActualInvoker, "Invoker must be created.");
            Assert.AreNotEqual(AppDomain.CurrentDomain.FriendlyName, 
                (appDomainInvoker.ActualInvoker as MockEngineInvoker).DomainFriendlyName,
                "Engine must be invoked in new domain.");
        }

        private class TestableEngineInvoker : AppDomainEngineInvoker<MockEngineInvoker>
        {
            public TestableEngineInvoker(string testSourcePath) : base(testSourcePath)
            {
            }

            public AppDomain NewAppDomain => this.appDomain;

            public IEngineInvoker ActualInvoker => this.actualInvoker;
        }

        private class MockEngineInvoker : MarshalByRefObject, IEngineInvoker
        {
            public string DomainFriendlyName { get; private set; }

            public void Invoke(IDictionary<string, string> argsDictionary)
            {
                this.DomainFriendlyName = AppDomain.CurrentDomain.FriendlyName;
            }
        }
    }
#endif
}
