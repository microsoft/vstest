// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.EventHandlers
{
    using TestPlatform.ObjectModel;
    using TestPlatform.ObjectModel.Engine;

    internal class TestCaseEventsHandler : ITestCaseEventsHandler
    {
        private TestRequestHandler requestHandler;

        public TestCaseEventsHandler(TestRequestHandler requestHandler)
        {
            this.requestHandler = requestHandler;
        }

        public void SendTestCaseStart(TestCase testCase)
        {
            throw new System.NotImplementedException();
        }

        public void SendTestCaseEnd(TestCase testCase, TestOutcome outcome)
        {
            throw new System.NotImplementedException();
        }

        public void SendTestResult(TestResult result)
        {
            throw new System.NotImplementedException();
        }
    }
}