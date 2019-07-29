using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger
{
    
    public class TestResult
    {
        public string FullyQualifiedName;
        public string DisplayName;
        public string ErrorStackTrace;
        public string ErrorMessage;
        public TestOutcome resultOutcome;
        

        public TimeSpan Duration { get; set; }
        public List<TestResult> innerTestResults;
        internal int GetInnerTestResultscount()
        {
            return this.innerTestResults.Count;
        }
        internal TestResult GetTestResult()
        {
            return this;
        }
    }
}
