﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    internal class MSTestV1TelemetryHelper
    {
        private static TestProperty testTypeProperty;
        private static TestProperty extensionIdProperty;

        internal static bool IsMSTestV1Adapter(Uri executorUri)
        {
            return IsMSTestV1Adapter(executorUri.AbsoluteUri);
        }

        internal static bool IsMSTestV1Adapter(string executorUri) {
            return string.Equals(executorUri, "executor://mstestadapter/v1", StringComparison.OrdinalIgnoreCase);
        }

        internal static void AddTelemetry(TestResult testResult, IDictionary<string, int> adapterTelemetry)
        {
            var executorUri = testResult?.TestCase?.ExecutorUri;
            // add additional info for mstestadapter/v1
            if (IsMSTestV1Adapter(executorUri))
            {
                if (testTypeProperty == null)
                {
                    // this is present when the legacy runner is used, and contains a guid which
                    // is the test type.
                    // GenericTestType 982B8C01-1A8A-48F5-B98A-67EE64BC8687
                    // OrderedTestType ec4800e8-40e5-4ab3-8510-b8bf29b1904d
                    // UnitTestType 13CDC9D9-DDB5-4fa4-A97D-D965CCFC6D4B
                    // WebTestType 4e7599fa-5ecb-43e9-a887-cd63cf72d207
                    // CodedWebTestType 37e36796-fb51-4610-8d5c-e00ceaa68b9f
                    testTypeProperty = TestProperty.Register("TestType", "TestType", typeof(Guid), typeof(TestResult));
                }

                if (extensionIdProperty == null)
                {
                    extensionIdProperty = TestProperty.Register("ExtensionId", "ExtensionId", typeof(string), typeof(TestResult));
                }
                // Get addional data from test result passed by MSTestv1
                // Only legacy tests have testtype.

                var testType = testResult.GetPropertyValue<Guid>(testTypeProperty, Guid.Empty);
                var hasTestType = testType != Guid.Empty;

                string key;
                if (hasTestType)
                {
                    var testExtension = testResult.GetPropertyValue<string>(extensionIdProperty, null);
                    var hasExtension = !string.IsNullOrWhiteSpace(testExtension);

                    if (hasExtension && testExtension.StartsWith("urn:"))
                    {
                        // remove urn: prefix
                        testExtension = testExtension.Remove(0, 4);
                    }

                    if (hasExtension)
                    {
                        key = $"{testResult.TestCase.ExecutorUri.AbsoluteUri}.legacy.extension.{testExtension}.count";
                    }
                    else
                    {
                        key = $"{testResult.TestCase.ExecutorUri.AbsoluteUri}.legacy.count";
                    }
                }
                else
                {
                    key = $"{testResult.TestCase.ExecutorUri.AbsoluteUri}.count";
                }

                if (adapterTelemetry.ContainsKey(key))
                {
                    adapterTelemetry[key]++;
                }
                else
                {
                    adapterTelemetry[key] =  1;
                }
            }
        }
    }
}
