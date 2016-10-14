// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;
    using System.IO;

    /// <summary>
    /// Represents required and optional information needed for requesting a file transfer.
    /// </summary>
    public class FileTransferInformation : BasicTransferInformation
    {
        #region Constructor

        /// <summary>
        /// Initializes with the information required for sending the contents of a file.
        /// </summary>
        /// <param name="context">The context in which the file is being sent.  Cannot be null.</param>
        /// <param name="path">The path to the file on the local file system</param>
        /// <param name="deleteFile">True to automatically have the file removed after sending it.</param>
        public FileTransferInformation(DataCollectionContext context, string path, bool deleteFile)
            : base(context)
        {
            //EqtAssert.StringNotNullOrEmpty(path, "path");

            // Expand environment variables in the path
            path = Environment.ExpandEnvironmentVariables(path);

            // Make sure the file exists.
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(string.Format(Resources.Resources.Common_FileNotExist, new object[] { path }), path);
            }

            // Make sure the path we have is a full path (not relative).
            Path = System.IO.Path.GetFullPath(path);

            PerformCleanup = deleteFile;
        }

        #endregion

        #region  Required Parameters.

        /// <summary>
        /// The path to the file on the local file system.
        /// </summary>
        public string Path { get; private set; }


        /// <summary>
        /// Indicates if cleanup should be performed after transferring the resource.
        /// </summary>
        protected internal override bool PerformCleanup { get; }

        /// <summary>
        /// The name of the file to use on the client machine.
        /// </summary>
        protected internal override string ClientFileName
        {
            get { return Path; }
        }

        #endregion
    }
}
