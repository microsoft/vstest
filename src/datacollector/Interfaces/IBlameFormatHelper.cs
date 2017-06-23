using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces
{
    public interface IBlameFormatHelper
    {
        void InitializeHelper();

        void AddTestToFormat(TestCase testCase);

        void SaveToFile(string filePath);

        TestCase ReadFaultyTestCase(string filePath);
    }
}
