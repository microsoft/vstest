// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces
{
    /// <summary>
    /// Helper class for getting info about dotnet host.
    /// </summary>
    public interface IDotnetHostHelper
    {
        /// <summary>
        /// Gets the full path for of .net core host.
        /// </summary>
        /// <returns>Full path to <c>dotnet</c> executable</returns>
        /// <remarks>Debuggers require the full path of executable to launch it.</remarks>
        string GetDotnetPath();

        /// <summary>
        /// Gets the full path of mono host.
        /// </summary>
        /// <returns>Full path to <c>mono</c> executable</returns>
        string GetMonoPath();
    }
}
