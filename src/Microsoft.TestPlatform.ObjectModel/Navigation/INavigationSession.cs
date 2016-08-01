// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Navigation
{
    using System;

    /// <summary>
    /// Manages the debug data associated with the .exe/.dll file
    /// </summary>
    public interface INavigationSession : IDisposable
    {
        /// <summary>
        /// Gets the navigation data for a method.
        /// </summary>
        /// <param name="declaringTypeName"> The declaring type name. </param>
        /// <param name="methodName"> The method name. </param>
        /// <returns> The <see cref="INavigationData"/> to get to the method. </returns>
        INavigationData GetNavigationDataForMethod(string declaringTypeName, string methodName);
    }
}
