// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Windows")]
    public class ProcessCountingTests
    {
        [TestMethod]
        public async Task ProcessCountIncludesRootChildrenAndGrandchildrenButNotUnrelatedProcesses()
        {
            using (var invocation = new ProcessInvocation())
            using (var cancellation = new CancellationTokenSource())
            {
                var count = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(cancellation, "powershell", invocation);
                using (var owned = new ProcessTree(2))
                using (var unrelated = new ProcessTree(1))
                {
                    invocation.SetRoot(owned.Root);
                    Assert.IsFalse(invocation.HasExited);
                    Assert.IsTrue(SpinWait.SpinUntil(() => invocation.ObservedCount == 3 || count.IsCompleted, 10000));
                    cancellation.Cancel();
                    Assert.AreEqual(3, (await count).Count);
                }
                Assert.IsTrue(invocation.HasExited, "The observed root must remain identifiable after its Process wrapper is disposed.");
            }
        }

        [TestMethod]
        public async Task ProcessCountCanExcludeAnAlreadyStartedTranslationLayerRoot()
        {
            using (var invocation = new ProcessInvocation())
            using (var cancellation = new CancellationTokenSource())
            using (var owned = new ProcessTree(0))
            {
                invocation.SetRoot(owned.Root);
                var count = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(cancellation, "powershell", invocation, includeRoot: false);
                owned.AddChild();
                owned.AddChild();
                Assert.IsTrue(SpinWait.SpinUntil(() => invocation.ObservedCount == 2 || count.IsCompleted, 10000));
                cancellation.Cancel();
                Assert.AreEqual(2, (await count).Count);
            }
        }

        [TestMethod]
        public async Task ProcessCountExcludesChildrenThatPredateObservation()
        {
            using (var invocation = new ProcessInvocation())
            using (var cancellation = new CancellationTokenSource())
            using (var owned = new ProcessTree(1))
            {
                invocation.SetRoot(owned.Root);
                var count = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(cancellation, "powershell", invocation, includeRoot: false);
                owned.AddChild();
                Assert.IsTrue(SpinWait.SpinUntil(() => invocation.ObservedCount == 1 || count.IsCompleted, 10000));
                cancellation.Cancel();
                Assert.AreEqual(1, (await count).Count);
            }
        }

        [TestMethod]
        public async Task ProcessCountStillRejectsAGenuineExtraOwnedProcess()
        {
            using (var invocation = new ProcessInvocation())
            using (var cancellation = new CancellationTokenSource())
            {
                var count = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(cancellation, "powershell", invocation);
                using (var owned = new ProcessTree(0))
                {
                    invocation.SetRoot(owned.Root);
                    Assert.IsTrue(SpinWait.SpinUntil(() => invocation.ObservedCount == 1 || count.IsCompleted, 10000));
                    owned.AddChild();
                    Assert.IsTrue(SpinWait.SpinUntil(() => invocation.ObservedCount == 2 || count.IsCompleted, 10000));
                    cancellation.Cancel();
                    var actual = (await count).Count;
                    Assert.AreEqual(2, actual);
                    Assert.ThrowsException<AssertFailedException>(() => Assert.AreEqual(1, actual));
                }
            }
        }

        [TestMethod]
        public async Task ProcessCountStillRejectsAThirdDirectWorker()
        {
            using (var invocation = new ProcessInvocation())
            using (var cancellation = new CancellationTokenSource())
            using (var owned = new ProcessTree(0))
            {
                invocation.SetRoot(owned.Root);
                var count = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(cancellation, "powershell", invocation, includeRoot: false);
                owned.AddChild();
                owned.AddChild();
                Assert.IsTrue(SpinWait.SpinUntil(() => invocation.ObservedCount == 2 || count.IsCompleted, 10000));
                owned.AddChild();
                Assert.IsTrue(SpinWait.SpinUntil(() => invocation.ObservedCount == 3 || count.IsCompleted, 10000));
                cancellation.Cancel();
                var actual = (await count).Count;
                Assert.AreEqual(3, actual);
                Assert.ThrowsException<AssertFailedException>(() => Assert.AreEqual(2, actual));
            }
        }

        [TestMethod]
        public async Task ProcessCountingWithoutAnAssignedRootFailsExplicitly()
        {
            using (var invocation = new ProcessInvocation())
            using (var cancellation = new CancellationTokenSource())
            {
                var count = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(cancellation, "powershell", invocation);
                cancellation.Cancel();
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await count);
            }
        }

        [TestMethod]
        public void ParentLifetimeRejectsRecycledProcessIds()
        {
            var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.IsTrue(NumberOfProcessLaunchedUtility.IsValidParentLifetime(start, start.AddSeconds(2), start.AddSeconds(1)));
            Assert.IsFalse(NumberOfProcessLaunchedUtility.IsValidParentLifetime(start, start.AddSeconds(2), start.AddSeconds(3)));
            Assert.IsFalse(NumberOfProcessLaunchedUtility.IsValidParentLifetime(start.AddSeconds(3), null, start.AddSeconds(1)));
        }

        private sealed class ProcessTree : IDisposable
        {
            internal Process Root { get; }

            internal ProcessTree(int depth)
            {
                var executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                    "WindowsPowerShell", "v1.0", "powershell.exe");
                var script = Encode(Script(executable));
                var startInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = "-NoLogo -NoProfile -NonInteractive -EncodedCommand " + script,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                startInfo.EnvironmentVariables["VSTEST_COUNT_TREE_DEPTH"] = depth.ToString();
                startInfo.EnvironmentVariables["VSTEST_COUNT_TREE_SCRIPT"] = script;
                Root = Process.Start(startInfo);
                try { ReadReady(); }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            internal void AddChild()
            {
                Root.StandardInput.WriteLine("extra");
                Root.StandardInput.Flush();
                ReadReady();
            }

            private void ReadReady()
            {
                var ready = Root.StandardOutput.ReadLineAsync();
                Assert.IsTrue(ready.Wait(10000), "The owned process tree did not reach its synchronization point.");
                Assert.IsTrue(int.TryParse(ready.Result, out _), "The process tree exited before it was ready.");
            }

            public void Dispose()
            {
                if (!Root.HasExited)
                {
                    Root.StandardInput.WriteLine("stop");
                    Root.StandardInput.Flush();
                    if (!Root.WaitForExit(10000))
                    {
                        Root.Kill();
                        Assert.Fail("The owned process tree did not stop.");
                    }
                }
                Root.Dispose();
            }

            private static string Encode(string script) => Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

            private static string Script(string executable)
            {
                return "$ErrorActionPreference='Stop';$depth=[int]$env:VSTEST_COUNT_TREE_DEPTH;"
                    + "function StartChild([int]$depth){$info=New-Object System.Diagnostics.ProcessStartInfo;"
                    + "$info.FileName='" + executable.Replace("'", "''") + "';"
                    + "$info.Arguments='-NoLogo -NoProfile -NonInteractive -EncodedCommand '+$env:VSTEST_COUNT_TREE_SCRIPT;"
                    + "$info.EnvironmentVariables['VSTEST_COUNT_TREE_DEPTH']=[string]$depth;"
                    + "$info.UseShellExecute=$false;$info.RedirectStandardInput=$true;$info.RedirectStandardOutput=$true;"
                    + "$child=[Diagnostics.Process]::Start($info);$null=$child.StandardOutput.ReadLine();return $child};"
                    + "$children=@();try{if($depth -gt 0){$children+=,(StartChild ($depth-1))};"
                    + "[Console]::WriteLine($PID);while(($command=[Console]::ReadLine()) -eq 'extra'){"
                    + "$children+=,(StartChild 0);[Console]::WriteLine($PID)}}finally{"
                    + "foreach($child in $children){$child.StandardInput.WriteLine('stop');$child.StandardInput.Flush();"
                    + "if(-not $child.WaitForExit(10000)){$child.Kill();throw 'Owned child did not stop'};$child.Dispose()}}";
            }
        }
    }
}
