// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector;
    
    [DataCollectorFriendlyName("CustomDataCollector")]
    [DataCollectorTypeUri("my://custom/datacollector")]
    public class CustomDataCollector : DataCollector, ITestExecutionEnvironmentSpecifier
    {
        public static bool IsInitialized = false;
        public static bool IsSessionStartedInvoked = false;
        public static bool IsSessionEndedInvoked = false;
        public static bool GetTestExecutionEnvironmentVariablesThrowException;
        public static IEnumerable<KeyValuePair<string, string>> EnvVarList;
        public static bool IsGetTestExecutionEnvironmentVariablesInvoked;
        public static bool ThrowExceptionWhenInitialized;
        public static bool IsDisposeInvoked;
        public static bool DisposeShouldThrowException;
        public static bool Events_SessionStartThrowException;
        public static string Events_SessionStartExceptionMessage = "SessionStartException";
        public static bool Attachfile = false;

        public DataCollectionEnvironmentContext dataCollectionEnvironmentContext { get; set; }

        public DataCollectionSink dataSink { get; set; }

        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {
            if (ThrowExceptionWhenInitialized)
            {
                throw new Exception("DataCollectorException");
            }

            IsInitialized = true;
            this.dataCollectionEnvironmentContext = environmentContext;
            this.dataSink = dataSink;
            events.SessionStart += new EventHandler<SessionStartEventArgs>(SessionStarted_Handler);
            events.SessionEnd += new EventHandler<SessionEndEventArgs>(SessionEnded_Handler);
        }

        public static void Reset()
        {
            IsInitialized = false;
            IsSessionStartedInvoked = false;
            IsSessionEndedInvoked = false;
            ThrowExceptionWhenInitialized = false;
            IsDisposeInvoked = false;
            IsGetTestExecutionEnvironmentVariablesInvoked = false;
            GetTestExecutionEnvironmentVariablesThrowException = false;
            DisposeShouldThrowException = false;
            EnvVarList = null;
            Events_SessionStartThrowException = false;
            File.Delete(Path.Combine(AppContext.BaseDirectory, "filename.txt"));
            Attachfile = false;
        }

        private void SessionStarted_Handler(object sender, SessionStartEventArgs args)
        {
            IsSessionStartedInvoked = true;

            if (Events_SessionStartThrowException)
            {
                throw new Exception(Events_SessionStartExceptionMessage);
            }

            if (Attachfile)
            {
                var filename = Path.Combine(AppContext.BaseDirectory, "filename.txt");
                File.WriteAllText(filename, string.Empty);
                this.dataSink.SendFileAsync(dataCollectionEnvironmentContext.SessionDataCollectionContext, filename, true);
            }
        }

        private void SessionEnded_Handler(object sender, SessionEndEventArgs args)
        {
            IsSessionEndedInvoked = true;
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

        protected override void Dispose(bool disposing)
        {
            IsDisposeInvoked = true;
            if (DisposeShouldThrowException)
            {
                throw new Exception("DataCollectorException");
            }
        }
    }

    [DataCollectorFriendlyName("CustomDataCollector")]
    public class CustomDataCollectorWithoutUri : DataCollector
    {
        public static bool IsInitialized = false;

        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {
            IsInitialized = true;
        }
    }

    [DataCollectorTypeUri("my://custom/datacollector")]
    public class CustomDataCollectorWithoutFriendlyName : DataCollector
    {
        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {
        }
    }

    [DataCollectorFriendlyName("")]
    [DataCollectorTypeUri("my://custom/datacollector")]
    public class CustomDataCollectorWithEmptyFriendlyName : DataCollector
    {
        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {
        }
    }
}