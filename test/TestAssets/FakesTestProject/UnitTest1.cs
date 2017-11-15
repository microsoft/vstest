
namespace FakesTestProject
{
    using System;
    using Microsoft.QualityTools.Testing.Fakes;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            int fixedYear = 2000;

            // Shims can be used only in a ShimsContext:
            using (ShimsContext.Create())
            {
                // Arrange:
                // Shim DateTime.Now to return a fixed date:
                System.Fakes.ShimDateTime.NowGet =
                    () =>
                    { return new DateTime(fixedYear, 1, 1); };

                // Instantiate the component under test:
                var componentUnderTest = new MyComponent();

                // Act:
                int year = componentUnderTest.GetTheCurrentYear();

                // Assert:
                // This will always be true if the component is working:
                Assert.AreEqual(fixedYear, year);
            }
        }
    }
}
