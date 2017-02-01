// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.console.UnitTests.TestDoubles
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;

    internal class DummyTestLoggerManager : TestLoggerManager
    {
        public DummyTestLoggerManager() : this(new DummyLoggerEvents(TestSessionMessageLogger.Instance))
        {
        }

        public DummyTestLoggerManager(InternalTestLoggerEvents loggerEvents)
            : base(TestSessionMessageLogger.Instance, loggerEvents)
        {
        }

        public HashSet<String> GetInitializedLoggers
        {
            get
            {
                return InitializedLoggers;
            }
        }

        public static void Cleanup()
        {
            Instance = null;
        }
    }
}
