using Microsoft.VisualStudio.TestPlatform.DataCollection.V1.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.DataCollection.V1;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.Common;
using Microsoft.VisualStudio.TestTools.Execution;

namespace Microsoft.TestPlatform.DataCollection.V1.UnitTests
{
    internal class MockDataCollectionFileManager : IDataCollectionFileManager
    {
        public List<AttachmentSet> Attachments;
        public const string GetDataExceptionMessaage = "FileManagerExcpetion";
        public bool GetDataThrowException;

        public MockDataCollectionFileManager()
        {
            Attachments = new List<AttachmentSet>();
        }
        public void CloseSession(SessionId id)
        {
            //throw new NotImplementedException();
        }

        public void ConfigureSession(SessionId id, string outputDirectory)
        {
            throw new NotImplementedException();
        }

        public void DispatchMessage(DataCollectorDataMessage collectorDataMessage)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public List<AttachmentSet> GetData(DataCollectionContext collectionContext)
        {
            if (GetDataThrowException)
            {
                throw new Exception(GetDataExceptionMessaage);
            }

            return Attachments;
        }
    }
}
