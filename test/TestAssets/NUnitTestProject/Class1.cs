using NUnit.Framework;

namespace NUnitTestProject
{
    [TestFixture]
    public class NUnitTest1
    {
        [Test]
        public void PassTestMethod1()
        {
            Assert.AreEqual(5, 5);
        }

        [Test]
        public void FailTestMethod1()
        {
            Assert.AreEqual(5, 6);
        }
    }
}
