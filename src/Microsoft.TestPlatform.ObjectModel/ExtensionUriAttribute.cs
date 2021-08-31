// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Resources;

    /// <summary>
    /// This attribute is applied to extensions so they can be uniquely identified.
    /// It indicates the Uri which uniquely identifies the extension.  If this attribute
    /// is not provided on the extensions such as the Test Executor or Test Logger, then
    /// the extensions will not be used.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ExtensionUriAttribute : Attribute
    {
        #region Constructor

        /// <summary>
        /// Initializes with the Uri of the extension.
        /// </summary>
        /// <param name="extensionUri">The Uri of the extension</param>
        public ExtensionUriAttribute(string extensionUri)
        {
            if (string.IsNullOrWhiteSpace(extensionUri))
            {
                throw new ArgumentException(CommonResources.CannotBeNullOrEmpty, nameof(extensionUri));
            }

            ExtensionUri = extensionUri;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The Uri of the Test Executor.
        /// </summary>
        public string ExtensionUri { get; private set; }

        #endregion

    }
}
