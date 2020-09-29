using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces
{
    public interface IStartTestRunnerEventsHandler : ITestMessageEventHandler
    {
        void HandleStartTestRunnerCallback(int pid);
    }
}
