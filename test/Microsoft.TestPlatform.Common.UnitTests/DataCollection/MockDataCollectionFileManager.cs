// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.Common.UnitTests.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    internal class MockDataCollectionFileManager : IDataCollectionFileManager
    {
        public bool DispatchMessageThrowException = false;
        public bool IsDispatchMessageInvoked;
        public DataCollectorDataMessage DataCollectorDataMessage;

        public void CloseSession(SessionId id)
        {
        }

        public void ConfigureSession(SessionId id, string outputDirectory)
        {
            throw new NotImplementedException();
        }

        public void DispatchMessage(DataCollectorDataMessage collectorDataMessage)
        {
            this.IsDispatchMessageInvoked = true;
            this.DataCollectorDataMessage = collectorDataMessage;
            if (this.DispatchMessageThrowException)
            {
                throw new Exception();
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public List<AttachmentSet> GetData(DataCollectionContext dataCollectionContext)
        {
            throw new NotImplementedException();
        }
    }
}
