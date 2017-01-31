using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.Common.Logging;

namespace vstest.console.UnitTests.TestDoubles
{
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
