// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.TestUtilities
{
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;

    public sealed class TestConsoleWrapperContext
    {
        public TestConsoleWrapperContext(IVsTestConsoleWrapper vsTestConsoleWrapper, string logsDirPath)
        {
            VsTestConsoleWrapper = vsTestConsoleWrapper;
            LogsDirPath = logsDirPath;
        }

        public IVsTestConsoleWrapper VsTestConsoleWrapper { get; }
        public string LogsDirPath { get; }
    }
}
