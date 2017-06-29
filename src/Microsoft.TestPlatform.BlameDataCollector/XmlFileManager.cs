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
        public void AddTestsToFormat(List<TestCase> TestSequence, string filePath)
        {
            foreach (var testCase in TestSequence)
            {
                var testElement = doc.CreateElement(Constants.BlameTestNode);
                testElement.SetAttribute(Constants.TestNameAttribute, testCase.FullyQualifiedName);
                testElement.SetAttribute(Constants.TestSourceAttribute, testCase.Source);
                blameTestRoot.AppendChild(testElement);
            }
            doc.AppendChild(blameTestRoot);
            using (var stream = this.fileHelper.GetStream(filePath, FileMode.Create))
            {
                doc.Save(stream);
            }
        }

        /// <summary>
        /// Reads Faulty test case from file
        /// </summary>
        /// <param name="filepath">The path of saved file</param>
        /// <returns>Faulty test case</returns>
        public TestCase ReadLastTestCase(string filePath)
        {
            TestCase testCase = new TestCase();
            string testname = string.Empty;
            var doc = new XmlDocument();
            using (var stream = this.fileHelper.GetStream(filePath, FileMode.Open))
            {
                doc.Load(stream);
            }
            var root = doc.LastChild;
            testCase.FullyQualifiedName = root.LastChild.Attributes[Constants.TestNameAttribute].Value;
            testCase.Source = root.LastChild.Attributes[Constants.TestSourceAttribute].Value;
            return testCase;
        }
    }
}
