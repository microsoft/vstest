using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution
{
    internal class FrameworkHandleProxy 
#if NET46
    : MarshalByRefObject
#endif
    {
#if NET46
        [NonSerialized]
#endif
        private IFrameworkHandle frameworkHandle;

        public FrameworkHandleProxy(IFrameworkHandle frameworkHandle)
        {
            this.frameworkHandle = frameworkHandle;
        }

        public bool EnableShutdownAfterTestRun { get; set; }

        public int LaunchProcessWithDebuggerAttached(string filePath, string workingDirectory, string arguments, IDictionary<string, string> environmentVariables)
        {
            return this.frameworkHandle.LaunchProcessWithDebuggerAttached(filePath, workingDirectory, arguments, environmentVariables);
        }

        public void RecordAttachments(string attachmentSetsString)
        {
            var attachmentSets = JsonDataSerializer.Instance.Deserialize<IList<AttachmentSet>>(attachmentSetsString);
            this.frameworkHandle.RecordAttachments(attachmentSets);
        }

        public void RecordEnd(string testCaseString, TestOutcome outcome)
        {
            var testCase = JsonDataSerializer.Instance.Deserialize<TestCase>(testCaseString);
            this.frameworkHandle.RecordEnd(testCase, outcome);
        }

        public void RecordResult(string testResultString)
        {
            var testResult = JsonDataSerializer.Instance.Deserialize<TestResult>(testResultString);
            this.frameworkHandle.RecordResult(testResult);
        }

        public void RecordStart(string testCaseString)
        {
            var testCase = JsonDataSerializer.Instance.Deserialize<TestCase>(testCaseString);
            this.frameworkHandle.RecordStart(testCase);
        }

        public void SendMessage(TestMessageLevel testMessageLevel, string message)
        {
            this.frameworkHandle.SendMessage(testMessageLevel, message);
        }
    }

    internal class FrameworkHandleInAppDomain :
#if NET46
        MarshalByRefObject,
#endif
        IFrameworkHandle
    {
        private FrameworkHandleProxy actualFrameworkHandle;

        internal InProcDataCollectionExtensionManager inProcDataCollectionExtensionManager { get; private set; }

        public FrameworkHandleInAppDomain(FrameworkHandleProxy frameworkHandleProxy, string runSettings)
        {
            this.actualFrameworkHandle = frameworkHandleProxy;
            this.EnableShutdownAfterTestRun = frameworkHandleProxy.EnableShutdownAfterTestRun;

            this.inProcDataCollectionExtensionManager = new InProcDataCollectionExtensionManager(runSettings, frameworkHandleProxy);
        }

        public bool EnableShutdownAfterTestRun { get; set; }

        public int LaunchProcessWithDebuggerAttached(string filePath, string workingDirectory, string arguments, IDictionary<string, string> environmentVariables)
        {
            return this.actualFrameworkHandle.LaunchProcessWithDebuggerAttached(filePath, workingDirectory, arguments, environmentVariables);
        }

        public void RecordAttachments(IList<AttachmentSet> attachmentSets)
        {
            var attachmentSetsString = JsonDataSerializer.Instance.Serialize<IList<AttachmentSet>>(attachmentSets);
            this.actualFrameworkHandle.RecordAttachments(attachmentSetsString);
        }

        public void RecordEnd(TestCase testCase, TestOutcome outcome)
        {
            this.inProcDataCollectionExtensionManager.TriggerTestCaseEnd(testCase, outcome);
            var testCaseString = JsonDataSerializer.Instance.Serialize<TestCase>(testCase);
            this.actualFrameworkHandle.RecordEnd(testCaseString, outcome);
        }

        public void RecordResult(TestResult testResult)
        {
            if(this.inProcDataCollectionExtensionManager.TriggerUpdateTestResult(testResult))
            {
                var testResultString = JsonDataSerializer.Instance.Serialize<TestResult>(testResult);
                this.actualFrameworkHandle.RecordResult(testResultString);
            }
        }

        public void RecordStart(TestCase testCase)
        {
            this.inProcDataCollectionExtensionManager.TriggerTestCaseStart(testCase);
            var testCaseString = JsonDataSerializer.Instance.Serialize<TestCase>(testCase);
            this.actualFrameworkHandle.RecordStart(testCaseString);
        }

        public void SendMessage(TestMessageLevel testMessageLevel, string message)
        {
            this.actualFrameworkHandle.SendMessage(testMessageLevel, message);
        }
    }
}
