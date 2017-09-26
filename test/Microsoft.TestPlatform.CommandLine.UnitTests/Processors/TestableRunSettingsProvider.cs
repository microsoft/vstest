// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommandLine.UnitTests.Processors
{
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;

    internal class TestableRunSettingsProvider : IRunSettingsProvider
    {
        public RunSettings ActiveRunSettings
        {
            get;
            set;
        }

        public void SetActiveRunSettings(RunSettings runSettings)
        {
            this.ActiveRunSettings = runSettings;
        }
    }
}
