// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.Build.Tasks
{
    using System.Collections.Generic;

    using Microsoft.Build.Utilities;

    public class VSTestTask : Task
    {
        public string TestFileFullPath
        {
            get;
            set;
        }

        public string VSTestSetting
        {
            get;
            set;
        }

        public string VSTestTests
        {
            get;
            set;
        }

        public string VSTestTestAdapterPath
        {
            get;
            set;
        }

        public string VSTestPlatform
        {
            get;
            set;
        }

        public string VSTestFramework
        {
            get;
            set;
        }

        public string VSTestTestCaseFilter
        {
            get;
            set;
        }
        public string VSTestLogger
        {
            get;
            set;
        }

        public string VSTestListTests
        {
            get;
            set;
        }

        public string VSTestParentProcessId
        {
            get;
            set;
        }

        public string VSTestPort
        {
            get;
            set;
        }

        public override bool Execute()
        {
            var vsTestForwardingApp = new VSTestForwardingApp(this.CreateArgument());

            vsTestForwardingApp.Execute();
            return true;
        }

        private string AddDoubleQoutes(string x)
        {
            return "\"" + x + "\"";
        }

        private IEnumerable<string> CreateArgument()
        {
            var allArgs = new List<string>();

            if (!string.IsNullOrEmpty(this.VSTestSetting))
            {
                allArgs.Add("--settings:" + this.AddDoubleQoutes(this.VSTestSetting));
            }

            if (!string.IsNullOrEmpty(this.VSTestTests))
            {
                allArgs.Add("--tests:" + this.VSTestTests);
            }

            if (!string.IsNullOrEmpty(this.VSTestTestAdapterPath))
            {
                allArgs.Add("--testAdapterPath:" + this.AddDoubleQoutes(this.VSTestTestAdapterPath));
            }

            if (!string.IsNullOrEmpty(this.VSTestPlatform))
            {
                allArgs.Add("--platform:" + this.VSTestPlatform);
            }

            if (!string.IsNullOrEmpty(this.VSTestFramework))
            {
                allArgs.Add("--framework:" + this.AddDoubleQoutes(this.VSTestFramework));
            }

            if (!string.IsNullOrEmpty(this.VSTestTestCaseFilter))
            {
                allArgs.Add("--testCaseFilter:" + this.AddDoubleQoutes(this.VSTestTestCaseFilter));
            }

            if (!string.IsNullOrEmpty(this.VSTestLogger))
            {
                var loggers = this.VSTestLogger.Split(new[] { ';' });

                foreach (var logger in loggers)
                {
                    allArgs.Add("--logger:" + logger);
                }
            }

            if (!string.IsNullOrEmpty(this.VSTestListTests))
            {
                allArgs.Add("--listTests:" + this.VSTestListTests);
            }

            if (!string.IsNullOrEmpty(this.VSTestParentProcessId))
            {
                allArgs.Add("--parentProcessId:" + this.VSTestParentProcessId);
            }

            if (!string.IsNullOrEmpty(this.VSTestPort))
            {
                allArgs.Add("--port:" + this.VSTestPort);
            }

            return allArgs;
        }
    }
}
