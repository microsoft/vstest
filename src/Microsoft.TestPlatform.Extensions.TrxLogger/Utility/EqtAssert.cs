// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.Utility
{
    using System;
    using System.Diagnostics;
    using System.Globalization;

    using TrxLoggerResources = Microsoft.TestPlatform.Extensions.TrxLogger.Resources.TrxResource;

    /// <summary>
    /// Class to be used for parameter verification.
    /// </summary>
    internal sealed class EqtAssert
    {
        /// <summary>
        /// Do not instantiate.
        /// </summary>
        private EqtAssert()
        {
        }

        /// <summary>
        /// Verifies that the specified parameter is not null, Debug.Asserts and throws.
        /// </summary>
        /// <param name="expression">Expression to check</param>
        /// <param name="comment">Comment to write</param>
        public static void IsTrue(bool expression, string comment)
        {
            Debug.Assert(expression, comment);
            if (!expression)
            {
                throw new Exception(comment);
            }
        }

        /// <summary>
        /// Verifies that the specified parameter is not null, Debug.Asserts and throws.
        /// </summary>
        /// <param name="parameter">Parameter to check</param>
        /// <param name="parameterName">String - parameter name</param>
        public static void ParameterNotNull(object parameter, string parameterName)
        {
            AssertParameterNameNotNullOrEmpty(parameterName);
            Debug.Assert(parameter != null, string.Format(CultureInfo.InvariantCulture, "'{0}' is null", parameterName));
            if (parameter == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        /// <summary>
        /// Verifies that the specified string parameter is neither null nor empty, Debug.Asserts and throws.
        /// </summary>
        /// <param name="parameter">Parameter to check</param>
        /// <param name="parameterName">String - parameter name</param>
        public static void StringNotNullOrEmpty(string parameter, string parameterName)
        {
            AssertParameterNameNotNullOrEmpty(parameterName);
            Debug.Assert(!string.IsNullOrEmpty(parameter), string.Format(CultureInfo.InvariantCulture, "'{0}' is null or empty", parameterName));
            if (string.IsNullOrEmpty(parameter))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, TrxLoggerResources.Common_CannotBeNullOrEmpty));
            }
        }

        /// <summary>
        /// Asserts that the parameter name is not null or empty
        /// </summary>
        /// <param name="parameterName">The parameter name to verify</param>
        [Conditional("DEBUG")]
        private static void AssertParameterNameNotNullOrEmpty(string parameterName)
        {
            Debug.Assert(!string.IsNullOrEmpty(parameterName), "'parameterName' is null or empty");
        }
    }
}
