// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection
{
    using System;
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    /// <summary>
    /// Represents required and optional information needed for requesting a file transfer.
    /// </summary>
    public class FileTransferInformation : BasicTransferInformation
    {
        private readonly IFileHelper fileHelper;

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="FileTransferInformation"/> class.
        /// </summary>
        /// <param name="context">
        /// The context in which the file is being sent.  Cannot be null.
        /// </param>
        /// <param name="path">
        /// The path to the file on the local file system
        /// </param>
        /// <param name="deleteFile">
        /// True to automatically have the file removed after sending it.
        /// </param>
        public FileTransferInformation(DataCollectionContext context, string path, bool deleteFile)
            : this(context, path, deleteFile, new TestPlatform.Utilities.Helpers.FileHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileTransferInformation"/> class.
        /// </summary>
        /// <param name="context">
        /// The context in which the file is being sent.  Cannot be null.
        /// </param>
        /// <param name="path">
        /// The path to the file on the local file system
        /// </param>
        /// <param name="deleteFile">
        /// True to automatically have the file removed after sending it.
        /// </param>
        /// <param name="fileHelper">
        /// The file Helper.
        /// </param>
        [CLSCompliant(false)]
        public FileTransferInformation(DataCollectionContext context, string path, bool deleteFile, IFileHelper fileHelper)
            : base(context)
        {
            this.fileHelper = fileHelper;

            // Expand environment variables in the path
            path = Environment.ExpandEnvironmentVariables(path);

            // Make sure the file exists.
            if (!this.fileHelper.Exists(path))
            {
                throw new FileNotFoundException(string.Format(Resources.Resources.Common_FileNotExist, new object[] { path }), path);
            }

            // Make sure the path we have is a full path (not relative).
            this.Path = this.fileHelper.GetFullPath(path);

            this.PerformCleanup = deleteFile;
        }

        #endregion

        #region  Required Parameters.

        /// <summary>
        /// Gets the path to the file on the local file system.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Indicates if cleanup should be performed after transferring the resource.
        /// </summary>
        protected internal override bool PerformCleanup { get; }

        /// <summary>
        /// The name of the file to use on the client machine.
        /// </summary>
        protected internal override string FileName => this.Path;

        #endregion
    }
}
