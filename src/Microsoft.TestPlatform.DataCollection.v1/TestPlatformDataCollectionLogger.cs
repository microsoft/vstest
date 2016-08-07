// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.DataCollection.V1
{
    using System;
    using System.Diagnostics;
    using System.Globalization;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    using DataCollectionContext = Microsoft.VisualStudio.TestTools.Execution.DataCollectionContext;
    using DataCollectionLogger = Microsoft.VisualStudio.TestTools.Execution.DataCollectionLogger;
    using DataCollectorInformation = Microsoft.VisualStudio.TestTools.Execution.DataCollectorInformation;

    /// <summary>
    /// Class used by data collectors to send messages to the client
    /// </summary>
    internal sealed class TestPlatformDataCollectionLogger : DataCollectionLogger
    {
        #region Private Fields

        /// <summary>
        /// The data collector information.
        /// </summary>
        private readonly DataCollectorInformation dataCollectorInformation;

        /// <summary>
        /// The sink.
        /// </summary>
        private readonly IMessageSink sink;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="TestPlatformDataCollectionLogger"/> class. 
        /// </summary>
        /// <param name="sink">
        /// The underlying raw IMessageSink.  Cannot be null.
        /// </param>
        /// <param name="dataCollectorInformation">
        /// The data Collector Information.
        /// </param>
        internal TestPlatformDataCollectionLogger(IMessageSink sink, DataCollectorInformation dataCollectorInformation)
        {
            ValidateArg.NotNull(dataCollectorInformation, "dataCollectorInformation");
            ValidateArg.NotNull(sink, "sink");
            this.dataCollectorInformation = dataCollectorInformation;
            this.sink = sink;
        }

        #region Public Members

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="context">The context in which the message is being sent.</param>
        /// <param name="text">The error text.  Cannot be null.</param>
        public override void LogError(DataCollectionContext context, string text)
        {
            ValidateArg.NotNull(context, "context");
            ValidateArg.NotNull(text, "text");

            if (EqtTrace.IsErrorEnabled)
            {
                EqtTrace.Error(
                    "Data collector '{0}' logged the following error: {1}",
                    this.dataCollectorInformation.TypeUri,
                    text);
            }

            this.SendTextMessage(context, text, TestMessageLevel.Error);
        }

        /// <summary>
        /// Logs an error message for an exception.
        /// </summary>
        /// <param name="context">The context in which the message is being sent.</param>
        /// <param name="text">Text explaining the exception.  Cannot be null.</param>
        /// <param name="exception">The exception.  Cannot be null.</param>
        public override void LogError(DataCollectionContext context, string text, Exception exception)
        {
            ValidateArg.NotNull(context, "context");
            ValidateArg.NotNull(text, "text");
            ValidateArg.NotNull(exception, "exception");

            // Make sure the data collection context is not a derived data collection context.  This
            // is done to safeguard from 3rd parties creating their own data collection contexts.
            if (context.GetType() != typeof(DataCollectionContext))
            {
                throw new InvalidOperationException(Resource.WrongDataCollectionContextType);
            }

            if (EqtTrace.IsErrorEnabled)
            {
                EqtTrace.Error(
                    "Data collector '{0}' logged the following error:" + Environment.NewLine +
                        "Description:            {1}" + Environment.NewLine +
                        "Exception type:         {2}" + Environment.NewLine + "Exception message:      {3}"
                    + Environment.NewLine + "Exception stack trace:  {4}",
                    this.dataCollectorInformation.TypeUri,
                    text,
                    exception.GetType(),
                    exception.Message,
                    exception.StackTrace);
            }

            // Currently there is one type of DataCollectionMessage sent accross client for all message kind.
            // If required new type can be created for different message type.
            var message = string.Format(
                CultureInfo.CurrentCulture,
                Resource.ReportDataCollectorException,
                exception.GetType(),
                exception.Message,
                text);
            this.SendTextMessage(context, message, TestMessageLevel.Error);
        }

        /// <summary>
        /// Logs a warning.
        /// </summary>
        /// <param name="context">The context in which the message is being sent.</param>
        /// <param name="text">The warning text.  Cannot be null.</param>
        public override void LogWarning(DataCollectionContext context, string text)
        {
            ValidateArg.NotNull(context, "context");
            ValidateArg.NotNull(text, "text");
            EqtTrace.Warning(
                    "Data collector '{0}' logged the following warning: {1}",
                    this.dataCollectorInformation.TypeUri,
                text);

            this.SendTextMessage(context, text, TestMessageLevel.Warning);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// The send text message.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <param name="level">
        /// The level.
        /// </param>
        /// <exception cref="InvalidOperationException">Throws InvalidOperationException.
        /// </exception>
        private void SendTextMessage(DataCollectionContext context, string text, TestMessageLevel level)
        {
            ValidateArg.NotNull(context, "context");
            ValidateArg.NotNull(text, "text");

            Debug.Assert(
                level >= TestMessageLevel.Informational && level <= TestMessageLevel.Error,
                "Invalid level: " + level);

            // Make sure the data collection context is not a derived data collection context.  This
            // is done to safeguard from 3rd parties creating their own data collection contexts.
            if (context.GetType() != typeof(DataCollectionContext))
            {
                throw new InvalidOperationException(Resource.WrongDataCollectionContextType);
            }

            DataCollectionMessageEventArgs args = new DataCollectionMessageEventArgs(level, text);
            args.Uri = this.dataCollectorInformation.TypeUri;
            args.FriendlyName = this.dataCollectorInformation.FriendlyName;
            if (context.HasTestCase)
            {
                args.TestCaseId = context.TestExecId.Id;
            }

            this.sink.SendMessage(args);
        }

        #endregion
    }
}