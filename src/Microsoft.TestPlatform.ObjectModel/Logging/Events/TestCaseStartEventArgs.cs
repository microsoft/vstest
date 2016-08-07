using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging.Events
{
    /// <summary>
    /// EventArg used for raising testcase start event.
    /// </summary>
    public class TestCaseStartEventArgs : EventArgs
    {
        public TestCaseStartEventArgs(TestCase testCase)
        {
            ValidateArg.NotNull<TestCase>(testCase, "testCase");
            TestCase = testCase;
        }

        /// <summary>
        /// Test case
        /// </summary>
        public TestCase TestCase { get; private set; }
    }
}
