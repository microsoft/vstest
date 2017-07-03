// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.BlameDataCollector
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System.Collections.Generic;

    public class BlameDataReaderWriter
    {
        private IBlameFileManager blameFileManager;

        #region Constructor 

        public BlameDataReaderWriter(IBlameFileManager blameFileManager)
        {
            ValidateArg.NotNull<IBlameFileManager>(blameFileManager, nameof(blameFileManager));
            this.blameFileManager = blameFileManager;
        }

        #endregion

        /// <summary>
        /// Writes all tests in the test sequence to the file
        /// </summary>
        public void WriteTestsToFile(List<object> TestSequence, string filePath)
        {
            ValidateArg.NotNull<List<object>>(TestSequence, nameof(TestSequence));
            ValidateArg.NotNullOrEmpty(filePath, nameof(filePath));

            // Initialize Helper
            blameFileManager.InitializeHelper();
            // Adds all tests to file
            blameFileManager.AddTestsToFormat(TestSequence, filePath);
        }

        /// <summary>
        /// Reads all tests in the file
        /// </summary>
        public List<object> ReadAllTests(string filePath)
        {
            ValidateArg.NotNull<string>(filePath, nameof(filePath));

            return blameFileManager.GetAllTests(filePath);
        }
    }
}
