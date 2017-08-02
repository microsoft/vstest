// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests
{
    using Microsoft.VisualStudio.TestPlatform.Common.Hosting;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;

    public class TestableTestEngine : TestEngine
    {
        public TestableTestEngine()
            : base(TestRuntimeProviderManager.Instance)
        {
        }
    }
}
