// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Implementations
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    public class MockFileHelper : IFileHelper
    {
        public Func<string, bool> ExistsInvoker { get; set; }

        public bool Exists(string path)
        {
            if (this.ExistsInvoker != null)
            {
                return this.ExistsInvoker.Invoke(path);
            }

            return false;
        }
    }
}
