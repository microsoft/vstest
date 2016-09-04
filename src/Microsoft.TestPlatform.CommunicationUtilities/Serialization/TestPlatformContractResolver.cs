// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// JSON contract resolver for mapping test platform types.
    /// </summary>
    public class TestPlatformContractResolver : DefaultContractResolver
    {
        /// <inheritdoc/>
        protected override JsonContract CreateContract(Type objectType)
        {
            var contract = base.CreateContract(objectType);

            if (objectType == typeof(ITestRunStatistics))
            {
                contract.Converter = new TestRunStatisticsConverter();
            }

            return contract;
        }
    }
}