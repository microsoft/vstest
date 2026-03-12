
[TestClass]
public class Build : IntegrationTestBase
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext)
    {
        IntegrationTestBuild.BuildTestAssetsForIntegrationTests(testContext);
    }
}
