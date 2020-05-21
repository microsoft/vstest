// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    /// <summary>
    /// XmlReaderWriter class for reading and writing test sequences to file
    /// </summary>
    public class XmlReaderWriter : IBlameReaderWriter
    {
        /// <summary>
        /// The file helper.
        /// </summary>
        private readonly IFileHelper fileHelper;

        #region  Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlReaderWriter"/> class.
        /// </summary>
        internal XmlReaderWriter()
            : this(new FileHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlReaderWriter"/> class.
        /// Protected for testing purposes
        /// </summary>
        /// <param name="fileHelper">
        /// The file helper.
        /// </param>
        protected XmlReaderWriter(IFileHelper fileHelper)
        {
            this.fileHelper = fileHelper;
        }

        #endregion

        /// <summary>
        /// Writes test Sequence to file.
        /// Protected for testing purposes
        /// </summary>
        /// <param name="testSequence">
        /// Sequence of Guids
        /// </param>
        /// <param name="testObjectDictionary">
        /// Dictionary of test objects
        /// </param>
        /// <param name="filePath">
        /// The file Path.
        /// </param>
        /// <returns>File path</returns>
        public string WriteTestSequence(List<Guid> testSequence, Dictionary<Guid, BlameTestObject> testObjectDictionary, string filePath)
        {
            ValidateArg.NotNull(testSequence, nameof(testSequence));
            ValidateArg.NotNull(testObjectDictionary, nameof(testObjectDictionary));
            ValidateArg.NotNullOrEmpty(filePath, nameof(filePath));

            filePath = filePath + ".xml";

            // Writing test sequence
            var xmlDocument = new XmlDocument();
            var xmlDeclaration = xmlDocument.CreateNode(XmlNodeType.XmlDeclaration, string.Empty, string.Empty);
            var blameTestRoot = xmlDocument.CreateElement(Constants.BlameRootNode);
            xmlDocument.AppendChild(xmlDeclaration);

            foreach (var testGuid in testSequence)
            {
                if (testObjectDictionary.ContainsKey(testGuid))
                {
                    var testObject = testObjectDictionary[testGuid];

                    var testElement = xmlDocument.CreateElement(Constants.BlameTestNode);
                    testElement.SetAttribute(Constants.TestNameAttribute, testObject.FullyQualifiedName);
                    testElement.SetAttribute(Constants.TestDisplayNameAttribute, testObject.DisplayName);
                    testElement.SetAttribute(Constants.TestSourceAttribute, testObject.Source);
                    testElement.SetAttribute(Constants.TestCompletedAttribute, testObject.IsCompleted.ToString());

                    blameTestRoot.AppendChild(testElement);
                }
            }

            xmlDocument.AppendChild(blameTestRoot);
            using (var stream = this.fileHelper.GetStream(filePath, FileMode.Create))
            {
                xmlDocument.Save(stream);
            }

            return filePath;
        }

        /// <summary>
        /// Reads All test case from file
        /// </summary>
        /// <param name="filePath">The path of test sequence file</param>
        /// <returns>Test Case List</returns>
        public List<BlameTestObject> ReadTestSequence(string filePath)
        {
            ValidateArg.NotNull(filePath, nameof(filePath));

            if (!this.fileHelper.Exists(filePath))
            {
                throw new FileNotFoundException();
            }

            var testCaseList = new List<BlameTestObject>();
            try
            {
                // Reading test sequence
                var xmlDocument = new XmlDocument();
                using (var stream = this.fileHelper.GetStream(filePath, FileMode.Open))
                {
                    xmlDocument.Load(stream);
                }

                var root = xmlDocument.LastChild;
                foreach (XmlNode node in root)
                {
                    var testCase = new BlameTestObject
                    {
                        FullyQualifiedName =
                                               node.Attributes[Constants.TestNameAttribute].Value,
                        Source = node.Attributes[Constants.TestSourceAttribute].Value,
                        DisplayName = node.Attributes[Constants.TestDisplayNameAttribute].Value,
                        IsCompleted = node.Attributes[Constants.TestCompletedAttribute].Value == "True" ? true : false
                    };
                    testCaseList.Add(testCase);
                }
            }
            catch (XmlException xmlException)
            {
                EqtTrace.Warning("XmlReaderWriter : Exception ", xmlException);
            }

            return testCaseList;
        }
    }
}
