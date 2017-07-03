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

    public class XmlFileManager : IBlameFileManager
    {
        private XmlDocument doc;
        private XmlElement blameTestRoot;
        private IFileHelper fileHelper;
        public XmlFileManager()
            : this(new FileHelper())
        {
        }
        protected XmlFileManager(IFileHelper fileHelper)
        {
            this.fileHelper = fileHelper;
        }

        /// <summary>
        /// Initializes resources for writing to file
        /// </summary>
        public void InitializeHelper()
        {
            doc = new XmlDocument();
            var xmlDeclaration = doc.CreateNode(XmlNodeType.XmlDeclaration, string.Empty, string.Empty);
            blameTestRoot = doc.CreateElement(Constants.BlameRootNode);
            doc.AppendChild(xmlDeclaration);
        }

        /// <summary>
        /// Adds tests to document and saves document to file
        /// </summary>
        public void AddTestsToFormat(List<object> TestSequence, string filePath)
        {
            foreach (var testCase in TestSequence)
            {
                TestCase test = (TestCase)testCase;
                var testElement = doc.CreateElement(Constants.BlameTestNode);
                testElement.SetAttribute(Constants.TestNameAttribute, test.FullyQualifiedName);
                testElement.SetAttribute(Constants.TestSourceAttribute, test.Source);
                blameTestRoot.AppendChild(testElement);
            }
            doc.AppendChild(blameTestRoot);
            using (var stream = this.fileHelper.GetStream(filePath, FileMode.Create))
            {
                doc.Save(stream);
            }
        }

        /// <summary>
        /// Reads All test case from file
        /// </summary>
        /// <param name="filepath">The path of saved file</param>
        /// <returns>Test Case List</returns>
        public List<object> GetAllTests(string filePath) 
        {
            List<object> testCaseList = new List<object>();
            var doc = new XmlDocument();
            using (var stream = this.fileHelper.GetStream(filePath, FileMode.Open))
            {
                doc.Load(stream);
            }
            var root = doc.LastChild;
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
