// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;
    using System.ComponentModel;
    using System.IO;

    /// <summary>
    /// Class used by data collectors to send data to up-stream components
    /// (agent, controller, client, etc).
    /// </summary>
    public abstract class DataCollectionSink
    {
        #region Constructor

        /// <summary>
        /// Creates a DataCollectionSink
        /// </summary>
        protected DataCollectionSink()
        {
        }

        #endregion

        #region Events

        /// <summary>
        /// Called when sending of a file has completed.
        /// </summary>
        public abstract event AsyncCompletedEventHandler SendFileCompleted;

        /// <summary>
        /// Called when sending of a stream has completed.
        /// </summary>
        public abstract event AsyncCompletedEventHandler SendStreamCompleted;

        #endregion

        #region SendFileAsync

        /// <summary>
        /// Sends a file to up-stream components.
        /// </summary>
        /// <param name="context">The context in which the file is being sent.  Cannot be null.</param>
        /// <param name="path">the path to the file on the local file system</param>
        /// <param name="deleteFile">True to automatically have the file removed after sending it.</param>
        public void SendFileAsync(DataCollectionContext context, string path, bool deleteFile)
        {
            SendFileAsync(context, path, String.Empty, deleteFile);
        }

        /// <summary>
        /// Sends a file to up-stream components.
        /// </summary>
        /// <param name="context">The context in which the file is being sent.  Cannot be null.</param>
        /// <param name="path">the path to the file on the local file system</param>
        /// <param name="description">A short description of the data being sent.</param>
        /// <param name="deleteFile">True to automatically have the file removed after sending it.</param>
        public void SendFileAsync(DataCollectionContext context, string path, string description, bool deleteFile)
        {
            var fileInfo = new FileTransferInformation(context, path, deleteFile);
            fileInfo.Description = description;

            SendFileAsync(fileInfo);
        }

        /// <summary>
        /// Sends a file to up-stream components
        /// </summary>
        /// <param name="fileTransferInformation">Information about the file to be transferred.</param>
        public abstract void SendFileAsync(FileTransferInformation fileTransferInformation);

        #endregion

        #region SendStreamAsync

        /// <summary>
        /// Sends a stream to up-stream components.
        /// </summary>
        /// <param name="context">The context in which the stream is being sent.  Cannot be null.</param>
        /// <param name="stream">Stream to send.</param>
        /// <param name="fileName">File name to use for the data on the client.</param>
        /// <param name="closeStream">True to automatically have the stream closed when sending of the contents has completed.</param>
        public void SendStreamAsync(DataCollectionContext context, Stream stream, string fileName, bool closeStream)
        {
            SendStreamAsync(context, stream, fileName, String.Empty, closeStream);
        }

        /// <summary>
        /// Sends a stream to up-stream components.
        /// </summary>
        /// <param name="context">The context in which the stream is being sent.  Cannot be null.</param>
        /// <param name="stream">Stream to send.</param>
        /// <param name="fileName">File name to use for the data on the client.</param>
        /// <param name="description">A short description of the data being sent.</param>
        /// <param name="closeStream">True to automatically have the stream closed when sending of the contents has completed.</param>
        public void SendStreamAsync(DataCollectionContext context, Stream stream, string fileName, string description, bool closeStream)
        {
            var streamInfo = new StreamTransferInformation(context, stream, fileName, closeStream);
            streamInfo.Description = description;

            SendStreamAsync(streamInfo);
        }

        /// <summary>
        /// Sends a stream to up-stream components.
        /// </summary>
        /// <param name="streamTransferInformation">Information about the stream being transferred.</param>
        public abstract void SendStreamAsync(StreamTransferInformation streamTransferInformation);

        #endregion
    }
}
