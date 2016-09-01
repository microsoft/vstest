namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    internal interface IInProcDataCollector
    {
        /// <summary>
        /// AssemblyQualifiedName of the datacollector type
        /// </summary>
        string AssemblyQualifiedName { get; }

        /// <summary>
        /// Loads the DataCollector type 
        /// </summary>
        /// <param name="inProcDataCollectionSink">Sink object to send data</param>
        void LoadDataCollector(IDataCollectionSink inProcDataCollectionSink);

        /// <summary>
        /// Triggers InProcDataCollection Methods
        /// </summary>
        /// <param name="methodName">Name of the method to trigger</param>
        /// <param name="methodArg">Arguments for the method</param>
        void TriggerInProcDataCollectionMethod(string methodName, InProcDataCollectionArgs methodArg);
    }
}
