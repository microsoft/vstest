// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.BlameDataCollector
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System.Collections.Generic;

    public class BlameDataReaderWriter
    {
        private string filePath;
        private IBlameFileManager blameFileManager;
        private List<TestCase> TestSequence;

        #region Constructor
        public BlameDataReaderWriter()
        { }

        public BlameDataReaderWriter(string filePath, IBlameFileManager blameFileManager)
        {
            ValidateArg.NotNullOrWhiteSpace(filePath, "File Path");
            ValidateArg.NotNull<IBlameFileManager>(blameFileManager, "Blame File manager");

            this.filePath = filePath;
            this.blameFileManager = blameFileManager;
        }

        public BlameDataReaderWriter(List<TestCase> TestSequence, string filePath, IBlameFileManager blameFileManager)
        {
            ValidateArg.NotNullOrWhiteSpace(filePath, "File Path");
            ValidateArg.NotNull<IBlameFileManager>(blameFileManager, "Blame File manager");

            this.TestSequence = TestSequence;
            this.filePath = filePath;
            this.blameFileManager = blameFileManager;
        }

        #endregion

        /// <summary>
        /// Writes all tests in the test sequence to the file
        /// </summary>
        public void WriteTestsToFile()
        {
            // Initialize Helper
            blameFileManager.InitializeHelper();

            // Adds all tests to file
            blameFileManager.AddTestsToFormat(this.TestSequence, this.filePath);

        }

        /// <summary>
        /// Gets last test case from the file
        /// </summary>
        /// <returns>Last test case</returns>
        public TestCase GetLastTestCase()
        {
            return blameFileManager.ReadLastTestCase(this.filePath);
        }
    }
}
