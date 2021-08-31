// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Resources;

    /// <summary>
    /// This attribute is applied to Loggers so they can be uniquely identified.
    /// It indicates the Friendly Name which uniquely identifies the extension.
    /// This attribute is optional.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class FriendlyNameAttribute : Attribute
    {
        #region Constructor

        /// <summary>
        /// Initializes with the Friendly Name of the logger.
        /// </summary>
        /// <param name="friendlyName">The friendly name of the Logger</param>
        public FriendlyNameAttribute(string friendlyName)
        {
            if (string.IsNullOrWhiteSpace(friendlyName))
            {
                throw new ArgumentException(CommonResources.CannotBeNullOrEmpty, nameof(friendlyName));
            }

            FriendlyName = friendlyName;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The friendly Name of the Test Logger.
        /// </summary>
        public string FriendlyName { get; private set; }

        #endregion

    }
}
