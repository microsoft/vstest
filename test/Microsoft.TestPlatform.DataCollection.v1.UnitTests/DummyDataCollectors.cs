// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.DataCollection.V1.UnitTests
{
    using Microsoft.VisualStudio.TestTools.Execution;
    using System.Xml;
    using System;
    using System.Collections.Generic;


    [DataCollectorTypeUri("datacollector://Company/Product/Version")]
    [DataCollectorFriendlyName("Collect Log Files", false)]
    public class MockDataCollector : DataCollector, ITestExecutionEnvironmentSpecifier
    {
        public static bool IsInitializeInvoked;
        public static bool ThrowExceptionWhenInitialized;
        public static bool IsDisposeInvoked;
        public static bool IsGetTestExecutionEnvironmentVariablesInvoked;
        public static bool GetTestExecutionEnvironmentVariablesThrowException;
        public static bool DisposeShouldThrowException;
        public static IEnumerable<KeyValuePair<string, string>> EnvVarList;
        public static bool IsEvents_SessionStartInvoked;
        public static bool IsEvents_TestCaseStartInvoked;
        public static bool ConfigureTestCaseLevelEvents;
        public static bool IsEvents_SessionEndInvoked;
        public static bool IsEvents_TestCaseEndInvoked;

        public static void Reset()
        {
            IsInitializeInvoked = false;
            ThrowExceptionWhenInitialized = false;
            IsDisposeInvoked = false;
            IsGetTestExecutionEnvironmentVariablesInvoked = false;
            GetTestExecutionEnvironmentVariablesThrowException = false;
            DisposeShouldThrowException = false;
            EnvVarList = null;
            IsEvents_SessionStartInvoked = false;
            IsEvents_TestCaseStartInvoked = false;
            ConfigureTestCaseLevelEvents = false;
            IsEvents_SessionEndInvoked = false;
            IsEvents_TestCaseEndInvoked = false;
        }
        public override void Initialize(XmlElement configurationElement, DataCollectionEvents events, DataCollectionSink dataSink, DataCollectionLogger logger, DataCollectionEnvironmentContext environmentContext)
        {
            if (ThrowExceptionWhenInitialized)
            {
                throw new Exception("DataCollectorException");
            }
            IsInitializeInvoked = true;

            events.SessionStart += Events_SessionStart;
            events.SessionEnd += Events_SessionEnd;
            if (ConfigureTestCaseLevelEvents)
            {
                events.TestCaseStart += Events_TestCaseStart;
                events.TestCaseEnd += Events_TestCaseEnd;
            }
        }

        private void Events_TestCaseEnd(object sender, TestCaseEndEventArgs e)
        {
            IsEvents_TestCaseEndInvoked = true;
        }

        private void Events_SessionEnd(object sender, SessionEndEventArgs e)
        {
            IsEvents_SessionEndInvoked = true;
        }

        private void Events_TestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            IsEvents_TestCaseStartInvoked = true;
        }

        private void Events_SessionStart(object sender, SessionStartEventArgs e)
        {
            IsEvents_SessionStartInvoked = true;
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposeInvoked = true;
            if (DisposeShouldThrowException)
            {
                throw new Exception("DataCollectorException");
            }
        }

        public IEnumerable<KeyValuePair<string, string>> GetTestExecutionEnvironmentVariables()
        {
            if (GetTestExecutionEnvironmentVariablesThrowException)
            {
                throw new Exception("DataCollectorException");
            }

            IsGetTestExecutionEnvironmentVariablesInvoked = true;
            return EnvVarList;
        }
    }

    [DataCollectorTypeUri("datacollector://Company/Product/Version2")]
    [DataCollectorFriendlyName("Collect Log Files2", false)]
    public class MockDataCollector2 : DataCollector, ITestExecutionEnvironmentSpecifier
    {
        public static bool Events_SessionStartThrowException;
        public static bool Events_TestCaseStartThrowException;
        public static bool Events_SessionEndThrowException;
        public static bool Events_TestCaseEndThrowException;
        public static bool ConfigureTestCaseLevelEvents;
        public static string Events_SessionStartExceptionMessage = "SessionStartException";
        public static string Events_TestCaseStartExceptionMessage = "TestCaseStartException";
        public static string Events_SessionEndExceptionMessage = "SessionEndException";
        public static string Events_TestCaseEndExceptionMessage = "TestCaseEndException";


        public static void Reset()
        {
            Events_SessionStartThrowException = false;
            Events_TestCaseStartThrowException = false;
            Events_SessionEndThrowException = false;
            Events_TestCaseEndThrowException = false;
            ConfigureTestCaseLevelEvents = false;
        }

        public IEnumerable<KeyValuePair<string, string>> GetTestExecutionEnvironmentVariables()
        {
            return new List<KeyValuePair<string, string>>();
        }

        public override void Initialize(XmlElement configurationElement, DataCollectionEvents events, DataCollectionSink dataSink, DataCollectionLogger logger, DataCollectionEnvironmentContext environmentContext)
        {
            events.SessionStart += Events_SessionStart;
            events.SessionEnd += Events_SessionEnd;

            if (ConfigureTestCaseLevelEvents)
            {
                events.TestCaseStart += Events_TestCaseStart;
                events.TestCaseEnd += Events_TestCaseEnd;
            }
        }

        private void Events_TestCaseEnd(object sender, TestCaseEndEventArgs e)
        {
            if (Events_TestCaseEndThrowException)
            {
                throw new Exception(Events_TestCaseEndExceptionMessage);
            }
        }

        private void Events_TestCaseStart(object sender, TestCaseStartEventArgs e)
        {
            if (Events_TestCaseStartThrowException)
            {
                throw new Exception(Events_TestCaseStartExceptionMessage);
            }
        }

        private void Events_SessionEnd(object sender, SessionEndEventArgs e)
        {
            if (Events_SessionEndThrowException)
            {
                throw new Exception(Events_SessionEndExceptionMessage);
            }
        }

        private void Events_SessionStart(object sender, SessionStartEventArgs e)
        {
            if (Events_SessionStartThrowException)
            {
                throw new Exception(Events_SessionStartExceptionMessage);
            }
        }
    }


}
