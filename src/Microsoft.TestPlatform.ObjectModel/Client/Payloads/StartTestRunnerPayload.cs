using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads
{
    /// <summary>
    /// Class used to define the StartTestRunnerPayload sent by the Vstest.console translation layers into design mode.
    /// </summary>
    public class StartTestRunnerPayload
    {
        /// <summary>
        /// RunSettings used for starting the test runner.
        /// </summary>
        [DataMember]
        public IEnumerable<string> Sources { get; set; }

        /// <summary>
        /// RunSettings used for starting the test runner.
        /// </summary>
        [DataMember]
        public string RunSettings { get; set; }
    }
}
