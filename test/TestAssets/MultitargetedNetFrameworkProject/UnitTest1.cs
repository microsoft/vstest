// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;

using Xunit;

namespace MultitargetedNetFrameworkProject
{
    public class UnitTest1
    {

#if NET451
        public string TargetFramework { get; } = "NET451";
#endif

#if NET452
        public string TargetFramework { get; } = "NET452";
#endif

#if NET46
        public string TargetFramework { get; } = "NET46";
#endif

#if NET461
        public string TargetFramework { get; } = "NET461";
#endif

#if NET462
        public string TargetFramework { get; } = "NET462";
#endif

#if NET47
        public string TargetFramework { get; } = "NET47";
#endif

#if NET471
        public string TargetFramework { get; } = "NET471";
#endif

#if NET472
        public string TargetFramework { get; } = "NET472";
#endif

#if NET48
        public string TargetFramework { get; } = "NET48";
#endif

        // Using xUnit here because MSTest uses AppDomains by default and fixes this problem for us
        // as long as the appdomains are enabled and modern .NET Framework is installed.
        [Fact]
        public void FailsUntilNet462ButPassesOnNewerNetFramework()
        {
            var processName = Process.GetCurrentProcess().ProcessName;
            Exception exception = null;
            try
            {
                MemoryStream stream = new MemoryStream();
                SslStream sslStream = new SslStream(stream);

                // this throws SSLException on net451-net462, on net471 onwards it passes so we can use it to test that we target correctly
                sslStream.BeginAuthenticateAsClient("microsoft.com", null, SslProtocols.None, false, new AsyncCallback(ProcessInformation), null);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            var shouldThrowException = new[] { "NET451", "NET452", "NET46", "NET461", "NET462" }.Contains(TargetFramework);

            if (shouldThrowException && exception == null)
            {
                throw new Exception($"Expected the code above to throw exception, " +
                    $"because it fails when running in <= NET462 versions of .NET Framework, " +
                    $"and we are running in process that was compiled as {TargetFramework}, " +
                    $"and is called {processName}, but there was no exception.");
            }

            if (!shouldThrowException && exception != null)
            {
                throw new Exception($"Expected the code above to not throw an exception, " +
                    $"because it does not fail when running in > NET462 versions of .NET Framework, " +
                    $"and we are running in process that was compiled as {TargetFramework}, " +
                    $"and is called {processName}.");
            }
        }

        static void ProcessInformation(IAsyncResult result)
        {
        }
    }
}
