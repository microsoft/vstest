// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using TestPlatform.ObjectModel;

    /// <summary>
    /// Data collector implementation for unit test (Rocksteady and MSTest)
    /// </summary>
    internal class UnitTestDataCollector : DynamicCoverageDataCollectorImpl
    {
        // Lock for controlling access to activeIISSessionsList
        private object m_lock = new object();       

        // List of currently active IIS sessions
        private List<SessionId> activeIISSessions = new List<SessionId>();

        private int maxNumberOfSessions = 0;
        private DateTime firstSessionStart;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="isManualTest">Whether it's running a manual test</param>
        /// <param name="isExecutedRemotely">Whether it's running inside a remote device</param>
        /// <param name="isConnectedDevice">Whether it's running inside RTR</param>
        public UnitTestDataCollector(bool isManualTest, bool isExecutedRemotely) 
            : base(isManualTest, isExecutedRemotely)
        {
        }

        /// <summary>
        /// Session start
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        internal override void SessionStart(object sender, SessionStartEventArgs e)
        {
            base.SessionStart(sender, e);

            if (this.collectAspDotNet)
            {
                lock (m_lock)
                {
                    // Start vanguard only if this is the first run
                    // If another user starts run against the same environment
                    // just increase the count of active sessions
                    if (activeIISSessions.Count == 0)
                    {
                        firstSessionStart = DateTime.Now;
                        this.StartVanguard(e.Context);
                    }
                    
                    activeIISSessions.Add(e.Context.SessionId);                    
                    EqtTrace.Verbose("UnitTestDataCollector:SessionStart called for session {0} - Active IIS session count = {1}",e.Context.SessionId, activeIISSessions.Count);

                    if (maxNumberOfSessions < activeIISSessions.Count)
                    {
                        maxNumberOfSessions = activeIISSessions.Count;
                    }
                }
            }
            else
            {
                this.StartVanguard(e.Context);
            }
        }

        /// <summary>
        /// Session end
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        internal override void SessionEnd(object sender, SessionEndEventArgs e)
        {                        
            if (this.collectAspDotNet)
            {
                lock (m_lock)
                {
                    // Its possible that session end is called multiple times for a test run.
                    // Check if the session exists in the list of active IIS runs.
                    // If it does not then that means SessionEnd has been called more than once
                    // for that run
                    if (activeIISSessions.Contains(e.Context.SessionId))
                    {                        
                        activeIISSessions.Remove(e.Context.SessionId);
                        EqtTrace.Verbose("UnitTestDataCollector:SessionEnd called for session {0}. Active session count  = {1}", e.Context.SessionId, activeIISSessions.Count);

                        if (activeIISSessions.Count == 0)
                        {
                            this.StopVanguard(e.Context);
                        }
                        else
                        {
                            this.GetCoverageData(e.Context);                            
                        }                        
                    }
                }
            }
            else
            {
                this.StopVanguard(e.Context);
            }

            base.SessionEnd(sender, e);
        }
    }
}
