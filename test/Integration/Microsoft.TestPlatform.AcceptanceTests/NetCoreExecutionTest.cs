
namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /*[TestClass]*/
    //TODO enable netcore test when test asset project migrate to csproj
    public class NetCoreExecutionTest:ExecutionTests
    {
        [TestInitialize]
        public void SetTestFrameWork()
        {
            this.framework = ".NETCoreApp,Version=v1.0";
            this.testEnvironment.TargetFramework = "netcoreapp1.0";
        }
    }
}
