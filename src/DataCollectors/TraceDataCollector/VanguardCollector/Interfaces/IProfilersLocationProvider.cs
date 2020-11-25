// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage.Interfaces
{
    /// <summary>
    /// Interface to provide vanguard and CLR IE directory and path.
    /// </summary>
    internal interface IProfilersLocationProvider
    {
        /// <summary>
        /// Get path to vanguard exe
        /// </summary>
        /// <returns>Vanguard path</returns>
        string GetVanguardPath();

        /// <summary>
        /// Get path to x86 vanguard profiler
        /// </summary>
        /// <returns>Vanguard x86 profiler path</returns>
        string GetVanguardProfilerX86Path();

        /// <summary>
        /// Get path to x64 vanguard profiler config
        /// </summary>
        /// <returns>Vanguard x64 profiler config path</returns>
        string GetVanguardProfilerConfigX64Path();

        /// <summary>
        /// Get path to x86 vanguard profiler config
        /// </summary>
        /// <returns>Vanguard x86 profiler config path</returns>
        string GetVanguardProfilerConfigX86Path();

        /// <summary>
        /// Get path to x64 vanguard profiler
        /// </summary>
        /// <returns>Vanguard x64 profiler path</returns>
        string GetVanguardProfilerX64Path();

        /// <summary>
        /// Get path to x86 CLR Instrumentation Engine
        /// </summary>
        /// <returns>x86 CLR IE Path</returns>
        string GetClrInstrumentationEngineX86Path();

        /// <summary>
        /// Get path to x64 CLR Instrumentation Engine
        /// </summary>
        /// <returns>x64 CLR IE Path</returns>
        string GetClrInstrumentationEngineX64Path();

        /// <summary>
        /// Get path to Microsoft.VisualStudio.CodeCoverage.Shim library
        /// </summary>
        /// <returns>Path to Microsoft.VisualStudio.CodeCoverage.Shim library</returns>
        string GetCodeCoverageShimPath();
    }
}