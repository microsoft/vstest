// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

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

        /// <summary>
        /// Try to locate native muxer(dotnet).
        /// Native muxer is the one build for the below architecture
        /// </summary>
        /// <param name="processHandle">Handle of current process</param>
        /// <param name="path">path to the muxer</param>
        /// <returns>true if native muxer is found</returns>
        bool TryGetNativeMuxerPath(IntPtr processHandle, out string path);

        /// <summary>
        /// Try to locate muxer of specific architecture
        /// </summary>
        /// <param name="processHandle">Handle of current process</param>
        /// <param name="architecture">Specific architecture</param>
        /// <param name="path">path to the muxer</param>
        /// <returns>true if native muxer is found</returns>
        bool TryGetMuxerPath(IntPtr processHandle, PlatformArchitecture targetArchitecture, out string path);
    }
}
