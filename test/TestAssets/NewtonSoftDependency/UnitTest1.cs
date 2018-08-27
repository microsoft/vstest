
namespace NewtonSoftDependency
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    
    public class Account
    {
        public string Email { get; set; }
        public bool Active { get; set; }
    }

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            string json = @"{'Email': 'john@example.com', 'Active': true}";

            Account account = JsonConvert.DeserializeObject<Account>(json);

            Assert.AreEqual("john@example.com", account.Email);
        }
    }
}
