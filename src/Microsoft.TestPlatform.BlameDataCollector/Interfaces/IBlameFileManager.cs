// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.BlameDataCollector
{
    using System.Collections.Generic;

    public interface IBlameFileManager
    {
        /// <summary>
        /// Initializes resources for writing to file
        /// </summary>
        void InitializeHelper();

        /// <summary>
        /// Adds tests to document
        /// </summary>
        /// <param name="testSequence">List of tests in sequence</param>
        /// <param name="filePath">The path of saved file</param>
        void AddTestsToFormat(List<object> testSequence, string filePath);

        /// <summary>
        /// Reads all tests from file
        /// </summary>
        /// <param name="filePath">The path of saved file</param>
        /// <returns>All tests</returns>
        List<object> GetAllTests(string filePath);
    }
}
