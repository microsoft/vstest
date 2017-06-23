
using Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.TestPlatform.DataCollector
{
    public class BlameDataReaderWriter
    {
        private string filePath;
        private IBlameFormatHelper blameFormatHelper;
        private List<TestCase> TestSequence;

        public BlameDataReaderWriter()
        { }

        public BlameDataReaderWriter(string filePath, IBlameFormatHelper blameFormatHelper)
        {
            this.filePath = filePath;
            this.blameFormatHelper = blameFormatHelper;
        }

        public BlameDataReaderWriter(List<TestCase> TestSequence, string filePath, IBlameFormatHelper blameFormatHelper)
        {
            this.TestSequence = TestSequence;
            this.filePath = filePath;
            this.blameFormatHelper = blameFormatHelper;
        }

        public void WriteTestsToFile()
        {
            blameFormatHelper.InitializeHelper();

            foreach (var test in TestSequence)
            {
                blameFormatHelper.AddTestToFormat(test);
            }
            blameFormatHelper.SaveToFile(this.filePath);
        }

        public TestCase GetLastTestCase()
        {
            return blameFormatHelper.ReadFaultyTestCase(this.filePath);
        }
    }
}

