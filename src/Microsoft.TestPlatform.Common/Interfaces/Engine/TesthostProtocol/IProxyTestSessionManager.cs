﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.Interfaces.Engine.TesthostProtocol
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <summary>
    /// 
    /// </summary>
    public interface IProxyTestSessionManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="skipDefaultAdapters"></param>
        void Initialize(bool skipDefaultAdapters);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="eventsHandler"></param>
        void StartTestSession(StartTestSessionCriteria criteria, ITestSessionEventsHandler eventsHandler);

        /// <summary>
        /// 
        /// </summary>
        void Abort();

        /// <summary>
        /// 
        /// </summary>
        void Close();
    }
}
