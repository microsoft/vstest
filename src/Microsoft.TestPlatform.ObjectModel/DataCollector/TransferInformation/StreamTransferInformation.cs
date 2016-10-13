// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;
    using System.IO;

    /// <summary>
    /// Represents required and optional information needed for requesting a stream transfer.
    /// </summary>
    public class StreamTransferInformation : BasicTransferInformation
    {
        #region Constructor

        /// <summary>
        /// Initializes with the with required information for sending the contents of a stream.
        /// </summary>
        /// <param name="context">The context in which the file is being sent.  Cannot be null.</param>
        /// <param name="stream">Stream to send.</param>
        /// <param name="fileName">File name to use for the data on the client.</param>
        /// <param name="closeStream">True to automatically have the stream closed when sending of the contents has completed.</param>
        public StreamTransferInformation(DataCollectionContext context, Stream stream, string fileName, bool closeStream)
            : base(context)
        {
            //todo
            //EqtAssert.ParameterNotNull(stream, "stream");

            // Make sure the trimmed filename is not empty.
            if ((fileName == null) ||
                (fileName = fileName.Trim()).Length == 0)
            {
                throw new ArgumentException(Resources.Resources.Common_CannotBeNullOrEmpty, "fileName");
            }

            // Make sure the filename provided is not a reserved filename.
            if (FileHelper.IsReservedFileName(fileName))
            {
                throw new ArgumentException(string.Format(Resources.Resources.DataCollectionSink_ReservedFilenameUsed, new object[] { fileName }), "fileName");
            }

            // Make sure just the filename was provided.
            string invalidCharacters;
            if (!FileHelper.IsValidFileName(fileName, out invalidCharacters))
            {
                throw new ArgumentException(string.Format(Resources.Resources.DataCollectionSink_InvalidFileNameCharacters, new object[] { fileName, invalidCharacters }), "fileName");
            }

            // If we can not read the stream, throw.
            if (!stream.CanRead)
            {
                throw new InvalidOperationException(Resources.Resources.DataCollectionSink_CanNotReadStream);
            }

            Stream = stream;
            FileName = fileName;
            PerformCleanup = closeStream;
        }

        #endregion

        #region  Required Parameters.

        /// <summary>
        /// Stream to send.
        /// </summary>
        public Stream Stream { get; private set; }

        /// <summary>
        /// File name to use for the data on the client.
        /// </summary>
        public string FileName { get; private set; }


        /// <summary>
        /// Indicates if cleanup should be performed after transferring the resource.
        /// </summary>
        protected internal override bool PerformCleanup { get; }

        /// <summary>
        /// The name of the file to use on the client machine.
        /// </summary>
        protected internal override string ClientFileName
        {
            get { return FileName; }
        }

        #endregion
    }
}
