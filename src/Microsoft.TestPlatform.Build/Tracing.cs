
using System;

namespace Microsoft.TestPlatform.Build.Trace
{
    public static class Tracing
    {
        public static bool traceEnabled = false;
        public static void Trace(string message)
        {
            if (traceEnabled)
            {
                Console.WriteLine(message);
            }
        }
    }
}
