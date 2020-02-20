using System.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrashingOnDebugAssertTestProject
{
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
