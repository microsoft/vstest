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
    }
}
