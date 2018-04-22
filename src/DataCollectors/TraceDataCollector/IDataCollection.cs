// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceCollector
{
    using System;
    using System.ComponentModel;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    #region Data Collection wrapping
    // This wrapping layer is here so that the collector is testable. The classes used by the actual implementation are
    // not publicly constructable and do not use interfaces.

    internal interface IDataCollectionEvents
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
        String TestCaseName { get; }
        TestCase TestElement { get; }
    }

    internal sealed class DataCollectionEnvironmentContextWrapper : IDataCollectionAgentContext
    {
        readonly DataCollectionEnvironmentContext _environmentContext;
        public DataCollectionEnvironmentContextWrapper(DataCollectionEnvironmentContext environmentContext)
        {
            _environmentContext = environmentContext;
        }

        public DataCollectionContext SessionDataCollectionContext { get { return _environmentContext.SessionDataCollectionContext; } }
    }

    internal sealed class DataCollectionEventsWrapper : IDataCollectionEvents
    {
        readonly DataCollectionEvents _wrapped;
        public DataCollectionEventsWrapper(DataCollectionEvents wrapped)
        {
            _wrapped = wrapped;
        }

        #region IDataCollectionEvents Members

        event EventHandler<SessionEndEventArgs> IDataCollectionEvents.SessionEnd
        {
            add { _wrapped.SessionEnd += value; }
            remove { _wrapped.SessionEnd -= value; }
        }

        event EventHandler<SessionStartEventArgs> IDataCollectionEvents.SessionStart
        {
            add { _wrapped.SessionStart += value; }
            remove { _wrapped.SessionStart -= value; }
        }

        event EventHandler<TestCaseEndEventArgs> IDataCollectionEvents.TestCaseEnd
        {
            add { _wrapped.TestCaseEnd += value; }
            remove { _wrapped.TestCaseEnd -= value; }
        }

        event EventHandler<TestCaseStartEventArgs> IDataCollectionEvents.TestCaseStart
        {
            add { _wrapped.TestCaseStart += value; }
            remove { _wrapped.TestCaseStart -= value; }
        }

        #endregion
    }

    internal sealed class DataCollectionSinkWrapper : IDataCollectionSink
    {
        readonly DataCollectionSink _wrapped;
        public DataCollectionSinkWrapper(DataCollectionSink wrapped)
        {
            _wrapped = wrapped;
        }

        #region IDataCollectionSink Members

        event AsyncCompletedEventHandler IDataCollectionSink.SendFileCompleted
        {
            add { _wrapped.SendFileCompleted += value; }
            remove { _wrapped.SendFileCompleted -= value; }
        }

        void IDataCollectionSink.SendFileAsync(DataCollectionContext context, string path, bool deleteFile)
        {
            _wrapped.SendFileAsync(context, path, deleteFile);
        }

        void IDataCollectionSink.SendFileAsync(DataCollectionContext context, string path, string description, bool deleteFile)
        {
            _wrapped.SendFileAsync(context, path, description, deleteFile);
        }

        void IDataCollectionSink.SendFileAsync(FileTransferInformation fileInformation)
        {
            _wrapped.SendFileAsync(fileInformation);
        }

        #endregion
    }

    internal sealed class DataCollectionLoggerWrapper : IDataCollectionLogger
    {
        readonly DataCollectionLogger _wrapped;
        public DataCollectionLoggerWrapper(DataCollectionLogger wrapped)
        {
            _wrapped = wrapped;
        }

        #region IDataCollectionLogger Members

        void IDataCollectionLogger.LogError(DataCollectionContext context, Exception exception)
        {
            _wrapped.LogError(context, exception);
        }

        void IDataCollectionLogger.LogError(DataCollectionContext context, string text)
        {
            _wrapped.LogError(context, text);
        }

        void IDataCollectionLogger.LogError(DataCollectionContext context, string text, Exception exception)
        {
            _wrapped.LogError(context, text, exception);
        }

        void IDataCollectionLogger.LogWarning(DataCollectionContext context, string text)
        {
            _wrapped.LogWarning(context, text);
        }

        #endregion
    }

    internal sealed class TestCaseStartEventArgsWrapper : ITestCaseContextEventArgs
    {
        readonly TestCaseStartEventArgs _args;
        public TestCaseStartEventArgsWrapper( TestCaseStartEventArgs e )
        {
            _args = e;
        }

        public DataCollectionContext Context { get { return _args.Context; } }
        public bool IsChildTestCase { get { return _args.IsChildTestCase; } }
        //public Int32 TcmTestCaseId { get { return _args.TcmInformation == null ? 0 : _args.TcmInformation.TestCaseId; } }
        public Guid TestCaseId { get { return _args.TestCaseId; } }
        public String TestCaseName { get { return _args.TestCaseName; } }
        public Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase TestElement { get { return _args.TestElement; } }
    }

    internal sealed class TestCaseEndEventArgsWrapper : ITestCaseContextEventArgs
    {
        readonly TestCaseEndEventArgs _args;
        public TestCaseEndEventArgsWrapper( TestCaseEndEventArgs e )
        {
            _args = e;
        }

        public DataCollectionContext Context { get { return _args.Context; } }
        public bool IsChildTestCase { get { return _args.IsChildTestCase; } }
        public Guid TestCaseId { get { return _args.TestCaseId; } }
        public String TestCaseName { get { return _args.TestCaseName; } }
        //public Int32 TcmTestCaseId { get { return _args.TcmInformation == null ? 0 : _args.TcmInformation.TestCaseId; } }
        public TestCase TestElement { get { return _args.TestElement; } }
    }

    #endregion
}
