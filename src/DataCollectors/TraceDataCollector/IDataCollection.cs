// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceCollector
{
    using System;
    using System.ComponentModel;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    #region Data Collection wrapping

    /* This wrapping layer is here so that the collector is testable. The classes used by the actual implementation are
     not publicly constructable and do not use interfaces.*/

#pragma warning disable SA1649 // File name must match first type name

#pragma warning disable SA1402 // File may only contain a single class
    internal interface IDataCollectionEvents
#pragma warning restore SA1649 // File name must match first type name
    {
        event EventHandler<SessionEndEventArgs> SessionEnd;

        event EventHandler<SessionStartEventArgs> SessionStart;

        event EventHandler<TestCaseEndEventArgs> TestCaseEnd;

        event EventHandler<TestCaseStartEventArgs> TestCaseStart;
    }

    internal interface IDataCollectionSink
    {
        event AsyncCompletedEventHandler SendFileCompleted;

        void SendFileAsync(DataCollectionContext context, string path, bool deleteFile);

        void SendFileAsync(DataCollectionContext context, string path, string displayName, bool deleteFile);

        void SendFileAsync(FileTransferInformation fileInformation);
    }

    internal interface IDataCollectionLogger
    {
        void LogError(DataCollectionContext context, Exception exception);

        void LogError(DataCollectionContext context, string text);

        void LogError(DataCollectionContext context, string text, Exception exception);

        void LogWarning(DataCollectionContext context, string text);
    }

    internal interface IDataCollectionAgentContext
    {
        DataCollectionContext SessionDataCollectionContext { get; }
    }

    internal interface ITestCaseContextEventArgs
    {
        DataCollectionContext Context { get; }

        bool IsChildTestCase { get; }

        Guid TestCaseId { get; }

        string TestCaseName { get; }

        TestCase TestElement { get; }
    }

    internal sealed class DataCollectionEnvironmentContextWrapper : IDataCollectionAgentContext
    {
        private readonly DataCollectionEnvironmentContext environmentContext;

        public DataCollectionEnvironmentContextWrapper(DataCollectionEnvironmentContext environmentContext)
        {
            this.environmentContext = environmentContext;
        }

        public DataCollectionContext SessionDataCollectionContext
        {
            get { return this.environmentContext.SessionDataCollectionContext; }
        }
    }

    internal sealed class DataCollectionEventsWrapper : IDataCollectionEvents
    {
        private readonly DataCollectionEvents wrapped;

        public DataCollectionEventsWrapper(DataCollectionEvents wrapped)
        {
            this.wrapped = wrapped;
        }

        #region IDataCollectionEvents Members

        event EventHandler<SessionEndEventArgs> IDataCollectionEvents.SessionEnd
        {
            add { this.wrapped.SessionEnd += value; }
            remove { this.wrapped.SessionEnd -= value; }
        }

        event EventHandler<SessionStartEventArgs> IDataCollectionEvents.SessionStart
        {
            add { this.wrapped.SessionStart += value; }
            remove { this.wrapped.SessionStart -= value; }
        }

        event EventHandler<TestCaseEndEventArgs> IDataCollectionEvents.TestCaseEnd
        {
            add { this.wrapped.TestCaseEnd += value; }
            remove { this.wrapped.TestCaseEnd -= value; }
        }

        event EventHandler<TestCaseStartEventArgs> IDataCollectionEvents.TestCaseStart
        {
            add { this.wrapped.TestCaseStart += value; }
            remove { this.wrapped.TestCaseStart -= value; }
        }

        #endregion
    }

    internal sealed class DataCollectionSinkWrapper : IDataCollectionSink
    {
        private readonly DataCollectionSink wrapped;

        public DataCollectionSinkWrapper(DataCollectionSink wrapped)
        {
            this.wrapped = wrapped;
        }

        #region IDataCollectionSink Members

        event AsyncCompletedEventHandler IDataCollectionSink.SendFileCompleted
        {
            add { this.wrapped.SendFileCompleted += value; }
            remove { this.wrapped.SendFileCompleted -= value; }
        }

        void IDataCollectionSink.SendFileAsync(DataCollectionContext context, string path, bool deleteFile)
        {
            this.wrapped.SendFileAsync(context, path, deleteFile);
        }

        void IDataCollectionSink.SendFileAsync(DataCollectionContext context, string path, string description, bool deleteFile)
        {
            this.wrapped.SendFileAsync(context, path, description, deleteFile);
        }

        void IDataCollectionSink.SendFileAsync(FileTransferInformation fileInformation)
        {
            this.wrapped.SendFileAsync(fileInformation);
        }

        #endregion
    }

    internal sealed class DataCollectionLoggerWrapper : IDataCollectionLogger
    {
        private readonly DataCollectionLogger wrapped;

        public DataCollectionLoggerWrapper(DataCollectionLogger wrapped)
        {
            this.wrapped = wrapped;
        }

        #region IDataCollectionLogger Members

        void IDataCollectionLogger.LogError(DataCollectionContext context, Exception exception)
        {
            this.wrapped.LogError(context, exception);
        }

        void IDataCollectionLogger.LogError(DataCollectionContext context, string text)
        {
            this.wrapped.LogError(context, text);
        }

        void IDataCollectionLogger.LogError(DataCollectionContext context, string text, Exception exception)
        {
            this.wrapped.LogError(context, text, exception);
        }

        void IDataCollectionLogger.LogWarning(DataCollectionContext context, string text)
        {
            this.wrapped.LogWarning(context, text);
        }

        #endregion
    }

    internal sealed class TestCaseStartEventArgsWrapper : ITestCaseContextEventArgs
    {
        private readonly TestCaseStartEventArgs args;

        public TestCaseStartEventArgsWrapper(TestCaseStartEventArgs e)
        {
            this.args = e;
        }

        public DataCollectionContext Context
        {
            get { return this.args.Context; }
        }

        public bool IsChildTestCase
        {
            get { return this.args.IsChildTestCase; }
        }

        // public Int32 TcmTestCaseId { get { return _args.TcmInformation == null ? 0 : _args.TcmInformation.TestCaseId; } }
        public Guid TestCaseId
        {
            get { return this.args.TestCaseId; }
        }

        public string TestCaseName
        {
            get { return this.args.TestCaseName; }
        }

        public Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase TestElement
        {
            get { return this.args.TestElement; }
        }
    }

    internal sealed class TestCaseEndEventArgsWrapper : ITestCaseContextEventArgs
    {
        private readonly TestCaseEndEventArgs args;

        public TestCaseEndEventArgsWrapper(TestCaseEndEventArgs e)
        {
            this.args = e;
        }

        public DataCollectionContext Context
        {
            get { return this.args.Context; }
        }

        public bool IsChildTestCase
        {
            get { return this.args.IsChildTestCase; }
        }

        public Guid TestCaseId
        {
            get { return this.args.TestCaseId; }
        }

        public string TestCaseName
        {
            get { return this.args.TestCaseName; }
        }

        // public Int32 TcmTestCaseId { get { return _args.TcmInformation == null ? 0 : _args.TcmInformation.TestCaseId; } }
        public TestCase TestElement
        {
            get { return this.args.TestElement; }
        }
    }

    #endregion
#pragma warning restore SA1402 // File may only contain a single class
}
