// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.BlameDataCollector
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System.Collections.Generic;

    public interface IBlameReaderWriter
    {
        /// <summary>
        /// Writes tests to document
        /// </summary>
        /// <param name="testSequence">List of tests in sequence</param>
        /// <param name="filePath">The path of file</param>
        void WriteTestSequence(List<TestCase> testSequence, string filePath);

        /// <summary>
        /// Reads all tests from file
        /// </summary>
        /// <param name="filePath">The path of saved file</param>
        /// <returns>All tests</returns>
        List<TestCase> ReadTestSequence(string filePath);
    }
}
