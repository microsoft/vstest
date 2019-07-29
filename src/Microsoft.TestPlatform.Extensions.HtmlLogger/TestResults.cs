using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger;

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger
{
    [DataContract]
    [KnownType(typeof(TestResult))]
    public sealed class TestResults
    {
        /// <summary>
        /// 
        /// </summary>
        public TestResults()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        internal TestRunSummary Summary { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        internal List<string> RunLevelMessageInformational =  new List<string>();

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        internal List<string> RunLevelMessageErrorAndWarning = new List<string>();

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        internal List<TestResult> Results = new List<TestResult>();

        

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal int GetTestResultscount()
        {
            return this.Results.Count;
        }

    }
}
