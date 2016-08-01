// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.Interfaces
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public interface ITestDiscoveryEventsRegistrar
    {
        /// <summary>
        /// Registers to receive discovery events from discovery request.
        /// These events will then be broadcast to any registered loggers.
        /// </summary>
        /// <param name="discoveryRequest">The discovery request to register for events on.</param>
        void RegisterDiscoveryEvents(IDiscoveryRequest discoveryRequest);

        /// <summary>
        /// Unregister the events from the discovery request.
        /// </summary>
        /// <param name="discoveryRequest">The discovery request from which events should be unregistered.</param>
        void UnregisterDiscoveryEvents(IDiscoveryRequest discoveryRequest);
    }
}
