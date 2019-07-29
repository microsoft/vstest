namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger
{
    public class TestRunSummary
    {
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public int SkippedTests { get; set; }
    }
}