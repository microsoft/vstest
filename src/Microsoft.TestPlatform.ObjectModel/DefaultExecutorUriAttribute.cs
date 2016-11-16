// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Resources;

    /// <summary>
    /// This attribute is applied on the discoverers to inform the framework about their default executor. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DefaultExecutorUriAttribute : Attribute
    {
        #region Constructor

        /// <summary>
        /// Initializes with the Uri of the executor.
        /// </summary>
        /// <param name="defaultExecutorUri">The Uri of the executor</param>
        public DefaultExecutorUriAttribute(string executorUri)
        {
            if (string.IsNullOrWhiteSpace(executorUri))
            {
                throw new ArgumentException(CommonResources.CannotBeNullOrEmpty, "executorUri");
            }

            ExecutorUri = executorUri;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The Uri of the Test Executor.
        /// </summary>
        public string ExecutorUri { get; private set; }

        #endregion

    }
}
