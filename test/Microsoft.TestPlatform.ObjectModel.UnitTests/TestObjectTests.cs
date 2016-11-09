// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.ObjectModel.UnitTests
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestObjectTests
    {
        [TestMethod]
        public void TestCaseIdShouldReturnGuidWhenTestPropertiesIdIsSet()
        {
            TestCase testCase = new TestCase("DummyNS.DummyClass.DummyTest", new Uri("executor://mstestadapter/v1"), "C:\tests.dll");
            Guid expected = new Guid("{8167845C-9CDB-476F-9F2B-1B1C1FE01B7D}");
            testCase.SetPropertyValue(TestCaseProperties.Id, expected);

            var actual = testCase.Id;

            Assert.AreEqual(expected, actual);
        }
    }
}
