// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.BlameDataCollector
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    /// <summary>
    /// XmlReaderWriter class for reading and writing test sequences to file
    /// </summary>
    public class XmlReaderWriter : IBlameReaderWriter
    {
        private readonly IFileHelper fileHelper;

        #region  Constructor
        
        public XmlReaderWriter()
            : this(new FileHelper())
        { }
        
        public XmlReaderWriter(IFileHelper fileHelper)
        {
            this.fileHelper = fileHelper;
        }

        #endregion

        /// <summary>
        /// Adds tests to document and saves document to file
        /// </summary>
        public void WriteTestSequence(List<TestCase> testSequence, string filePath)
        {
            ValidateArg.NotNull(testSequence, nameof(testSequence));
            ValidateArg.NotNullOrEmpty(filePath, nameof(filePath));

            var xmlDocument = new XmlDocument();
            var xmlDeclaration = xmlDocument.CreateNode(XmlNodeType.XmlDeclaration, string.Empty, string.Empty);
            var blameTestRoot = xmlDocument.CreateElement(Constants.BlameRootNode);
            xmlDocument.AppendChild(xmlDeclaration);

            foreach (var testCase in testSequence)
            {
                var testElement = xmlDocument.CreateElement(Constants.BlameTestNode);
                testElement.SetAttribute(Constants.TestNameAttribute, testCase.FullyQualifiedName);
                testElement.SetAttribute(Constants.TestSourceAttribute, testCase.Source);
                blameTestRoot.AppendChild(testElement);
            }
            xmlDocument.AppendChild(blameTestRoot);

            using (var stream = this.fileHelper.GetStream(filePath, FileMode.Create))
            {
                xmlDocument.Save(stream);
            }
        }

        /// <summary>
        /// Reads All test case from file
        /// </summary>
        /// <param name="filePath">The path of saved file</param>
        /// <returns>Test Case List</returns>
        public List<TestCase> ReadTestSequence(string filePath) 
        {
            ValidateArg.NotNullOrEmpty(filePath, nameof(filePath));

            if (!fileHelper.Exists(filePath))
            {
                throw new FileNotFoundException();
            }

            List<TestCase> testCaseList = new List<TestCase>();
            var xmlDocument = new XmlDocument();
            using (var stream = this.fileHelper.GetStream(filePath, FileMode.Open))
            {
                xmlDocument.Load(stream);
            }
            var root = xmlDocument.LastChild;

            foreach (XmlNode node in root)
            {
                TestCase testCase = new TestCase();
                testCase.FullyQualifiedName = node.Attributes[Constants.TestNameAttribute].Value;
                testCase.Source = node.Attributes[Constants.TestSourceAttribute].Value;
                testCaseList.Add(testCase);
            }
            return testCaseList;
        }
    }
}
