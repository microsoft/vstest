// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace testhost.UnitTests
{
#if NETCOREAPP 
    using Microsoft.VisualStudio.TestPlatform.TestHost;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Diagnostics;

    [TestClass]
    public class TestHostTraceListenerTests
    {
        public TestHostTraceListenerTests()
        {
            // using this instead of class initialize to avoid crashing the whole process
            // in case we break the class init behavior due to some other changes
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new TestHostTraceListener());
        }

        [TestMethod]
        [ExpectedException(typeof(DebugAssertException))]
        public void DebugAssertThrowsDebugAssertException()
        {
            Debug.Assert(false);
        }

        [TestMethod]
        [ExpectedException(typeof(DebugAssertException))]
        public void DebugFailThrowsDebugAssertException()
        {
            Debug.Fail("fail");
        }

        [TestMethod]
        [ExpectedException(typeof(DebugAssertException))]
        public void TraceAssertThrowsDebugAssertException()
        {
            Trace.Assert(false);
        }

        [TestMethod]
        [ExpectedException(typeof(DebugAssertException))]
        public void TraceFailThrowsDebugAssertException()
        {
            Trace.Fail("fail");
        }

        [TestMethod]
        public void TraceWriteDoesNotFailTheTest()
        {
            Trace.Write("hello");
        }

        [TestMethod]
        public void TraceWriteLineDoesNotFailTheTest()
        {
            Trace.WriteLine("hello");
        }

        [TestMethod]
        public void DebugWriteDoesNotFailTheTest()
        {
            Debug.Write("hello");
        }

        [TestMethod]
        public void DebugWriteLineDoesNotFailTheTest()
        {
            Debug.WriteLine("hello");
        }
    }
#endif
}
