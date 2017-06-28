using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TestPlatform.BlameDataCollector
{
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
            blameFileManager.InitializeHelper();

            foreach (var test in TestSequence)
            {
                blameFileManager.AddTestToFormat(test);
            }
            blameFileManager.SaveToFile(this.filePath);
        }

        /// <summary>
        /// Gets faulty test case from the file
        /// </summary>
        /// <returns>Faulty test case</returns>
        public TestCase GetLastTestCase()
        {
            return blameFileManager.ReadFaultyTestCase(this.filePath);
        }
    }
}
