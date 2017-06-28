using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TestPlatform.BlameDataCollector
{
    public interface IBlameFileManager
    {

        /// <summary>
        /// Initializes resources for writing to file
        /// </summary>
        void InitializeHelper();

        /// <summary>
        /// Adds test to document
        /// </summary>
        void AddTestToFormat(TestCase testCase);

        /// <summary>
        /// Saves file to given file path
        /// </summary>
        /// <param name="filepath">The path where to save the file</param>
        void SaveToFile(string filePath);

        /// <summary>
        /// Reads Faulty test case from file
        /// </summary>
        /// <param name="filepath">The path of saved file</param>
        /// <returns>Faulty test case</returns>
        TestCase ReadFaultyTestCase(string filePath);
    }
}
