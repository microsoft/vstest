using System.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrashingOnDebugAssertTestProject
{
    // Release profile in this project needs to be the same as 
    // Debug profile, to define TRACE and DEBUG constants and don't
    // optimize the code, otherwise we will not run Debug.Assert in 
    // our acceptance tests on build server, because that is built as Release
    [TestClass]
    public class DebugTests
    {
        [TestMethod]
        public void DebugAssert()
        {
            Debug.Assert(false);
        }

        [TestMethod]
        public void DebugFail()
        {
            Debug.Fail("fail");
        }

        [TestMethod]
        public void TraceAssert()
        {
            Trace.Assert(false);
        }

        [TestMethod]
        public void TraceFail()
        {
            Trace.Fail("fail");
        }

        [TestMethod]
        [ExpectedException(typeof(DebugAssertException))]
        public void CatchingExceptionFromDebugAssert()
        {
            Debug.Assert(false);
        }
    }
}
