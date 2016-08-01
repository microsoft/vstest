// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    /// <summary>
    /// Type of test case failure which occured.
    /// </summary>
    public enum TestCaseFailureType
    {
        None = 0,
        Assertion = 1,
        UnhandledException = 2,
        UnexpectedException = 3,
        MissingException = 4,
        Other = 5,
    }
}
