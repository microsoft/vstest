// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.ObjectModel.UnitTests
{
    using System.Runtime.Versioning;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FrameworkTests
    {
        [TestMethod]
        public void FrameworkFromStringShouldTrimSpacesAroundFrameworkString()
        {
            var fx = Framework.FromString("  Framework35");

            Assert.AreEqual(".NETFramework,Version=v3.5", fx.Name);
            Assert.AreEqual("3.5", fx.Version);
        }

        [TestMethod]
        public void DefaultFrameworkShouldBeNet46OnDesktop()
        {
#if NET46
            Assert.AreEqual(".NETFramework,Version=v4.6", Framework.DefaultFramework.Name);
#endif
        }

        [TestMethod]
        public void DefaultFrameworkShouldBeNetCoreApp10OnNonDesktop()
        {
#if !NET46
            Assert.AreEqual(".NETCoreApp,Version=v1.0", Framework.DefaultFramework.Name);
#endif
        }
    }
}
