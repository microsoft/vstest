using Microsoft.VisualStudio.TestPlatform.DataCollector;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace vstest.console.Internal
{
    [FriendlyName(BlameLogger.FriendlyName)]
    [ExtensionUri(BlameLogger.ExtensionUri)]
    class BlameLogger : ITestLogger
    {
        public const string ExtensionUri = "logger://Microsoft/TestPlatform/BlameLogger";
        public const string FriendlyName = "Blame";
        //private bool isAborted;

        public BlameLogger()
        {
        }

        public void Initialize(TestLoggerEvents events, string testRunDictionary)
        {
            events.TestRunMessage += this.TestMessageHandler;
            events.TestRunComplete += this.TestRunCompleteHandler;
        }

        private void TestMessageHandler(object sender, TestRunMessageEventArgs e)
        {
            ValidateArg.NotNull<object>(sender, "sender");
            ValidateArg.NotNull<TestRunMessageEventArgs>(e, "e");

            if (e.Level == TestMessageLevel.Error && e.Message.Equals(""))
            {
                //isAborted = true;
            }
        }

        private void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
            BlameXmlHelper blameXmlhelper = new BlameXmlHelper();
            Console.WriteLine("Blame Logger working");
            Console.WriteLine(e.AttachmentSets[0].DisplayName);

            var dataReader = new BlameDataReaderWriter(e.AttachmentSets[0].Attachments[0].Uri.LocalPath, blameXmlhelper);
            var testCase = dataReader.GetLastTestCase();
            Console.WriteLine(testCase.FullyQualifiedName);
        }
    }
}
