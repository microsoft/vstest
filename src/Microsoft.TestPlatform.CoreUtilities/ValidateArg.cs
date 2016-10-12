// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // FileMayOnlyContainASingleClass. This is a ported class from ObjectModel.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.CoreUtilities;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Resources;

    /// <summary>
    /// Helper to validate parameters.
    /// </summary>
    public static class ValidateArg
    {
        /// <summary>
        /// Throws ArgumentNullException if the argument is null, otherwise passes it through.
        /// </summary>
        /// <typeparam name="T">
        /// Type to validate.
        /// </typeparam>
        /// <param name="arg">
        /// The argument to check.
        /// </param>
        /// <param name="parameterName">
        /// The parameter name of the argument.
        /// </param>
        /// <returns>
        /// Type of argument.
        /// </returns>
        [DebuggerStepThrough]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is shared source. This method may not be called in the current assembly.")]
        public static T NotNull<T>([ValidatedNotNull]T arg, string parameterName)
            where T : class
        {
            if (arg == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return arg;
        }

        /// <summary>
        /// Validate a string is not null or empty.
        /// </summary>
        /// <param name="arg">
        /// Input string.
        /// </param>
        /// <param name="parameterName">
        /// Name of the parameter to validate.
        /// </param>
        /// <returns>
        /// Validated string.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the input string is null or empty.
        /// </exception>
        [DebuggerStepThrough]
        public static string NotNullOrEmpty([ValidatedNotNull]string arg, string parameterName)
        {
            if (string.IsNullOrEmpty(arg))
            {
                throw new ArgumentNullException(parameterName);
            }

            return arg;
        }

        /// <summary>
        /// Throws ArgumentOutOfRangeException if the argument is less than zero.
        /// </summary>
        /// <param name="arg">The argument to check.</param>
        /// <param name="parameterName">The parameter name of the argument.</param>
        [DebuggerStepThrough]
        public static void NotNegative(int arg, string parameterName)
        {
            if (arg < 0)
            {
                var message = string.Format(CultureInfo.CurrentCulture, Resources.Error_ArgumentIsNegative);
                throw new ArgumentOutOfRangeException(parameterName, arg, message);
            }
        }

        /// <summary>
        /// Throws ArgumentOutOfRangeException if the argument is less than zero.
        /// </summary>
        /// <param name="arg">The argument to check.</param>
        /// <param name="parameterName">The parameter name of the argument.</param>
        [DebuggerStepThrough]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is shared source. This method may not be called in the current assembly.")]
        public static void NotNegative(long arg, string parameterName)
        {
            if (arg < 0)
            {
                var message = string.Format(CultureInfo.CurrentCulture, Resources.Error_ArgumentIsNegative);
                throw new ArgumentOutOfRangeException(parameterName, arg, message);
            }
        }

        /// <summary>
        /// Throws ArgumentNullException if the string is null, ArgumentException if the string is empty.
        /// </summary>
        /// <typeparam name="T">Type of parameter to validate.</typeparam>
        /// <param name="arg">The argument to check.</param>
        /// <param name="parameterName">The parameter name of the argument.</param>
        [DebuggerStepThrough]
        public static void NotNullOrEmpty<T>([ValidatedNotNull]IEnumerable<T> arg, string parameterName)
        {
            NotNull(arg, parameterName);

            if (!arg.Any())
            {
                var message = string.Format(CultureInfo.CurrentCulture, Resources.Error_ArgumentIsEmpty);
                throw new ArgumentException(message, parameterName);
            }
        }

        /// <summary>
        /// Throws ArgumentNullException if the argument is null, ArgumentException if the argument is not the correct type.
        /// </summary>
        /// <param name="arg">The argument to check.</param>
        /// <param name="parameterName">The parameter name of the argument.</param>
        /// <typeparam name="T">The type of the expected argument.</typeparam>
        [DebuggerStepThrough]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "This is shared source. This method may not be called in the current assembly.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is shared source. This method may not be called in the current assembly.")]
        public static void TypeOf<T>([ValidatedNotNull]object arg, string parameterName)
            where T : class
        {
            NotNull(arg, parameterName);

            if (!(arg is T))
            {
                var message = string.Format(CultureInfo.CurrentCulture, Resources.Error_ArgumentNotTypeOf, typeof(T).FullName);
                throw new ArgumentException(message, parameterName);
            }
        }
    }

    /// <summary>
    /// Helper to validate parameter properties.
    /// </summary>
    public static class ValidateArgProperty
    {
        /// <summary>
        /// Throws ArgumentException if the argument is null.
        /// </summary>
        /// <param name="arg">The argument to check (e.g. <c>Param1.PropertyA</c>).</param>
        /// <param name="parameterName">The parameter name of the argument.</param>
        /// <param name="propertyName">The property name of the argument.</param>
        [DebuggerStepThrough]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is shared source. This method may not be called in the current assembly.")]
        public static void NotNull([ValidatedNotNull]object arg, string parameterName, string propertyName)
        {
            if (arg == null)
            {
                var message = string.Format(CultureInfo.CurrentCulture, Resources.Error_ArgumentPropertyIsNull, propertyName);
                throw new ArgumentNullException(parameterName, message);
            }
        }

        /// <summary>
        /// Throws ArgumentException if the argument is less than zero.
        /// </summary>
        /// <param name="arg">The argument to check (e.g. <c>Param1.PropertyA</c>).</param>
        /// <param name="parameterName">The parameter name of the argument.</param>
        /// <param name="propertyName">The property name of the argument.</param>
        [DebuggerStepThrough]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is shared source. This method may not be called in the current assembly.")]
        public static void NotNegative(int arg, string parameterName, string propertyName)
        {
            if (arg < 0)
            {
                var message = string.Format(CultureInfo.CurrentCulture, Resources.Error_ArgumentPropertyIsNegative, propertyName);
                throw new ArgumentException(message, parameterName);
            }
        }

        /// <summary>
        /// Throws ArgumentException if the argument string is null or empty.
        /// </summary>
        /// <param name="arg">The argument to check (e.g. <c>Param1.PropertyA</c>).</param>
        /// <param name="parameterName">The parameter name of the argument.</param>
        /// <param name="propertyName">The property name of the argument.</param>
        [DebuggerStepThrough]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is shared source. This method may not be called in the current assembly.")]
        public static void NotNullOrEmpty([ValidatedNotNull]string arg, string parameterName, string propertyName)
        {
            NotNull(arg, parameterName, propertyName);

            if (string.IsNullOrEmpty(arg))
            {
                var message = string.Format(CultureInfo.CurrentCulture, Resources.Error_ArgumentPropertyIsEmpty, propertyName);
                throw new ArgumentException(message, parameterName);
            }
        }

        /// <summary>
        /// Throws ArgumentException if the argument is null or is not the correct type.
        /// </summary>
        /// <param name="arg">The argument to check (e.g. <c>Param1.PropertyA</c>).</param>
        /// <param name="parameterName">The parameter name of the argument.</param>
        /// <param name="propertyName">The property name of the argument.</param>
        /// <typeparam name="T">The type of the expected argument.</typeparam>
        [DebuggerStepThrough]
        public static void TypeOf<T>([ValidatedNotNull]object arg, string parameterName, string propertyName)
            where T : class
        {
            NotNull(arg, parameterName, propertyName);

            if (!(arg is T))
            {
                var message = string.Format(CultureInfo.CurrentCulture, Resources.Error_ArgumentPropertyNotTypeOf, propertyName, typeof(T).FullName);
                throw new ArgumentException(message, parameterName);
            }
        }
    }

    /// <summary>
    /// Secret attribute that tells the CA1062 validate arguments rule that this method validates the argument is not null.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class ValidatedNotNullAttribute : Attribute
    {
    }
}