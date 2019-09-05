using Microsoft.VisualStudio.TestPlatform.Common.Logging;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution
{
  public class TestExtenstionInitializeEventsHandler : ITestExtenstionInitializeEventsHandler
    {
        private ITestRequestHandler requestHandler;

        private TestSessionMessageLogger testRunHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestMessageEventHandler"/> class.
        /// </summary>
        /// <param name="requestHandler">test request handler</param>
        public TestExtenstionInitializeEventsHandler(ITestRequestHandler requestHandler)
        {
            this.requestHandler = requestHandler;
            testRunHandler = TestSessionMessageLogger.Instance;
        }

        public void SubscribetoSessionEvent()
        {
            testRunHandler.TestRunMessage += TestRunHandler_TestRunMessage;
        }

        public void UnSubscribetoSessionEvent()
        {
            testRunHandler.TestRunMessage -= TestRunHandler_TestRunMessage;
        }

        private void TestRunHandler_TestRunMessage(object sender, TestRunMessageEventArgs e)
        {
            HandleLogMessage(e.Level, e.Message);
        }

        /// <summary>
        /// Handles a test run message.
        /// </summary>
        /// <param name="level"> The level. </param>
        /// <param name="message"> The message. </param>
        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            switch ((TestMessageLevel)level)
            {
                case TestMessageLevel.Informational:
                    EqtTrace.Info(message);
                    break;

                case TestMessageLevel.Warning:
                    EqtTrace.Warning(message);
                    break;

                case TestMessageLevel.Error:
                    EqtTrace.Error(message);
                    break;

                default:
                    EqtTrace.Info(message);
                    break;
            }

            this.requestHandler.SendLog(level, message);
        }

        public void HandleRawMessage(string rawMessage)
        {
            // No-Op
            // TestHost at this point has no functionality where it requires rawmessage
        }

    }
}
