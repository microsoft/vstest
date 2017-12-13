// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Collections.Generic;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    /// <summary>
    /// Test result aggregation.
    /// </summary>
    internal class TestResultAggregation : TestResult, ITestResultAggregation
    {
        protected List<ITestResult> innerResults = new List<ITestResult>();

        public TestResultAggregation(
            string testName,
            string computerName,
            Guid runId,
            Guid executionId,
            Guid parentExecutionId,
            ITestElement test,
            TestOutcome outcome) : base(testName, computerName, runId, executionId, parentExecutionId, test, outcome) { }

        /// <summary>
        /// Gets the inner results.
        /// </summary>
        public List<ITestResult> InnerResults
        {
            get { return innerResults; }
        }

        public override void Save(System.Xml.XmlElement element, XmlTestStoreParameters parameters)
        {
            base.Save(element, parameters);

            XmlPersistence helper = new XmlPersistence();
            if (innerResults.Count > 0)
                helper.SaveIEnumerable(innerResults, element, "InnerResults", ".", null, parameters);
        }
    }
}
