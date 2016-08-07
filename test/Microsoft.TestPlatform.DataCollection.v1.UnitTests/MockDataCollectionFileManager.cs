// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.DataCollection.V1.UnitTests
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using VisualStudio.TestPlatform.Common.DataCollection;
    using VisualStudio.TestPlatform.Common.DataCollection.Interfaces;

    internal class MockDataCollectionFileManager : IDataCollectionFileManager
    {
        public List<AttachmentSet> Attachments;
        public const string GetDataExceptionMessaage = "FileManagerExcpetion";
        public bool GetDataThrowException;

        public MockDataCollectionFileManager()
        {
            this.Attachments = new List<AttachmentSet>();
        }

        public void CloseSession(SessionId id)
        {
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

        public List<AttachmentSet> GetData(DataCollectionContext dataCollectionContext)
        {
            if (this.GetDataThrowException)
            {
                throw new Exception(GetDataExceptionMessaage);
            }

            return this.Attachments;
        }
    }
}
