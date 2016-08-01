// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter
{
    /// <summary>
    /// Specifies the user specified RunSettings and framework provided context of the discovery. 
    /// </summary>
    public interface IDiscoveryContext
    {
        /// <summary>
        /// Runsettings specified for this request.
        /// </summary>
        IRunSettings RunSettings { get; }
    }
}
