using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrashingOnDebugAssertTestProject
{
    [TestClass]
    public class DebugTests
    {
        [TestMethod]
        public void AssertDebug()
        {
            Debug.Assert(true == false);
        }
    }
}
