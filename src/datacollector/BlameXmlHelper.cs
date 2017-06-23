using Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using FileHelper = Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.FileHelper;
using Constants = Microsoft.VisualStudio.TestPlatform.DataCollector.Constants;

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    public class BlameXmlHelper : IBlameFormatHelper
    {
        private XmlDocument doc;
        private XmlElement blameTestRoot;
        public BlameXmlHelper()
        {}

        public void InitializeHelper()
        {
            doc = new XmlDocument();
            var xmlDeclaration = doc.CreateNode(XmlNodeType.XmlDeclaration, string.Empty, string.Empty);
            blameTestRoot = doc.CreateElement(Constants.BlameRootNode);
            doc.AppendChild(xmlDeclaration);
        }
        public void AddTestToFormat(TestCase testCase)
        {
            var testElement = doc.CreateElement(Constants.BlameTestNode);
            testElement.SetAttribute(Constants.TestNameAttribute, testCase.FullyQualifiedName);
            testElement.SetAttribute(Constants.TestSourceAttribute, testCase.Source);
            blameTestRoot.AppendChild(testElement);
        }

        public void SaveToFile(string filePath)
        {
            doc.AppendChild(blameTestRoot);

            using (var stream = new FileHelper().GetStream(filePath, FileMode.Create))
            {
                doc.Save(stream);
            }
        }

        public TestCase ReadFaultyTestCase(string filePath)
        {
            TestCase testCase = new TestCase();
            string testname = string.Empty;
            var doc = new XmlDocument();
            using (var stream = new FileHelper().GetStream(filePath, FileMode.Open))
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
