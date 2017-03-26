// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    // <inheritdoc />
    internal class InProcDataCollectionSink : IDataCollectionSink
    {
        private IDictionary<Guid, TestCaseDataCollectionData> testCaseDataCollectionDataMap;

        /// <summary>
        /// In process data collection sink
        /// </summary>
        public InProcDataCollectionSink()
        {
            this.testCaseDataCollectionDataMap = new Dictionary<Guid, TestCaseDataCollectionData>();
        }

        // <inheritdoc />
        public void SendData(DataCollectionContext dataCollectionContext, string key, string value)
        {
            ValidateArg.NotNullOrEmpty(key, "key");
            ValidateArg.NotNullOrEmpty(value, "value");
            ValidateArg.NotNullOrEmpty(dataCollectionContext.TestCase.Id.ToString(), "dataCollectionContext.TestCase.Id");

            var testCaseId = dataCollectionContext.TestCase.Id;
            this.AddKeyValuePairToDictionary(testCaseId, key, value);
        }

        /// <summary>
        /// Gets the data collection data stored in the in process data collection sink
        /// </summary>
        /// <param name="testCaseId">valid test case id</param>
        /// <returns>test data collection dictionary </returns>
        public IDictionary<string, string> GetDataCollectionDataSetForTestCase(Guid testCaseId)
        {
            TestCaseDataCollectionData testCaseDataCollection = null;

            if (!this.testCaseDataCollectionDataMap.TryGetValue(testCaseId, out testCaseDataCollection))
            {
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning("No DataCollection Data set for the test case {0}", testCaseId);
                }

                return new Dictionary<string, string>();
            }
            else
            {
                this.testCaseDataCollectionDataMap.Remove(testCaseId);
                return testCaseDataCollection.CollectionData;
            }
        }

        private void AddKeyValuePairToDictionary(Guid testCaseId, string key, string value)
        {
            if (!this.testCaseDataCollectionDataMap.ContainsKey(testCaseId))
            {
                var testCaseCollectionData = new TestCaseDataCollectionData();
                testCaseCollectionData.AddOrUpdateData(key, value);
                this.testCaseDataCollectionDataMap[testCaseId] = testCaseCollectionData;
            }
            else
            {
                this.testCaseDataCollectionDataMap[testCaseId].AddOrUpdateData(key, value);
            }
        }

        private class TestCaseDataCollectionData
        {
            public TestCaseDataCollectionData()
            {
                this.CollectionData = new Dictionary<string, string>();
            }

            internal IDictionary<string, string> CollectionData { get; private set; }

            internal void AddOrUpdateData(string key, string value)
            {
                if (!this.CollectionData.ContainsKey(key))
                {
                    this.CollectionData[key] = value;
                }
                else
                {
                    if (EqtTrace.IsWarningEnabled)
                    {
                        EqtTrace.Warning("The data for inprocdata collector with key {0} has already been set. Will be reset with new value", key);
                    }
                    this.CollectionData[key] = value;
                }
            }
        }
    }
}
