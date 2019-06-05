using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace AppDomainGetAssembliesTestProject
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            // https://github.com/microsoft/vstest/issues/2008
            // GetAssemblies adds datacollectors to test host appdomain which was failing in the above issue

            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach(var assembly in allAssemblies)
            {
                var typeInfo = assembly.GetTypes();
            }
        }
    }
}
