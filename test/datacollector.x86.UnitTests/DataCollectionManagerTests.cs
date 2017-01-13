// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



namespace datacollector.x86.UnitTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.VisualStudio.TestPlatform.DataCollector;

    [TestClass]
    public class DataCollectionManagerTests
    {
        DataCollectionManager dataCollectorManager;

        [TestInitialize]
        public void Init()
        {
            dataCollectorManager = new DataCollectionManager();
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldThrowExceptionIfSettingsXmlIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.dataCollectorManager.Initialize(null);
            });
        }
    }
}
