using System.Reflection;
using System;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.InProcDataCollector;

[assembly: AssemblyKeyFileAttribute("key.snk")]

namespace Coverlet.Collector.DataCollection
{
    public class CoverletInProcDataCollector : InProcDataCollection
    {
        public void Initialize(IDataCollectionSink dataCollectionSink)
        {
            throw new NotImplementedException();
        }

        public void TestCaseEnd(TestCaseEndArgs testCaseEndArgs)
        {
            throw new NotImplementedException();
        }

        public void TestCaseStart(TestCaseStartArgs testCaseStartArgs)
        {
            throw new NotImplementedException();
        }

        public void TestSessionEnd(TestSessionEndArgs testSessionEndArgs)
        {
            throw new NotImplementedException();
        }

        public void TestSessionStart(TestSessionStartArgs testSessionStartArgs)
        {
            throw new NotImplementedException();
        }
    }
}
