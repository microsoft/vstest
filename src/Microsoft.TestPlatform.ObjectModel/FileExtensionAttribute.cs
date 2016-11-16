// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Resources;

    /// <summary>
    /// This attribute is applied to ITestDiscoverers.  It indicates
    /// which file extensions the test discoverer knows how to process.
    /// If this attribute is not provided on the test discoverer it will be
    /// called for all file types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class FileExtensionAttribute : Attribute
    {
        #region Constructor

        /// <summary>
        /// Initializes with a file extension that the test discoverer can process tests from. 
        /// For example ".dll" or ".exe".
        /// </summary>
        /// <param name="fileExtension">The file extensions that the test discoverer can process tests from.</param>
        public FileExtensionAttribute(string fileExtension)
        {
            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                throw new ArgumentException(CommonResources.CannotBeNullOrEmpty, "fileExtension");
            }

            FileExtension = fileExtension;
        }

        #endregion

        #region Properties

        /// <summary>
        /// A file extensions that the test discoverer can process tests from.  For example ".dll" or ".exe".
        /// </summary>
        public string FileExtension { get; private set; }

        #endregion
    }
}
