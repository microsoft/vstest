// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Common.PlatformTests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using global::TestPlatform.TestUtilities;

    [TestClass]
    public class ProcessTreeCounterTests
    {
        [TestMethod]
        public void CountsOnlyNewMatchingDescendantsRegardlessOfSnapshotOrder()
        {
            var counter = new NumberOfProcessLaunchedUtility.ProcessTreeCounter(10, "dotnet", new[] { 20 });
            var snapshot = new Dictionary<int, Tuple<int, string>>
            {
                { 31, Tuple.Create(30, "dotnet") },
                { 99, Tuple.Create(1, "dotnet") },
                { 30, Tuple.Create(10, "vstest.console") },
                { 20, Tuple.Create(10, "dotnet") },
                { 32, Tuple.Create(30, "DOTNET") },
            };

            counter.ObserveSnapshot(snapshot);
            counter.ObserveSnapshot(snapshot);

            Assert.AreEqual(2, counter.Count);
        }

        [TestMethod]
        public void RetainsObservedAncestryWhenAnIntermediateProcessExits()
        {
            var counter = new NumberOfProcessLaunchedUtility.ProcessTreeCounter(10, "testhost.x86", new int[0]);
            counter.ObserveSnapshot(new Dictionary<int, Tuple<int, string>> { { 20, Tuple.Create(10, "vstest.console") } });
            counter.ObserveSnapshot(new Dictionary<int, Tuple<int, string>>
            {
                { 30, Tuple.Create(20, "testhost.x86") },
                { 40, Tuple.Create(1, "testhost.x86") },
            });

            Assert.AreEqual(1, counter.Count);
        }

        [TestMethod]
        public void SelectedConsoleExcludesHostsFromSiblingSessions()
        {
            var snapshot = new Dictionary<int, Tuple<int, string>>
            {
                { 20, Tuple.Create(10, "vstest.console") },
                { 30, Tuple.Create(10, "vstest.console") },
                { 21, Tuple.Create(20, "testhost.x86") },
                { 31, Tuple.Create(30, "testhost.x86") },
                { 32, Tuple.Create(30, "testhost.x86") },
            };
            var wholeTest = new NumberOfProcessLaunchedUtility.ProcessTreeCounter(10, "testhost.x86", new int[0]);
            var selectedConsole = new NumberOfProcessLaunchedUtility.ProcessTreeCounter(30, "testhost.x86", new int[0]);

            wholeTest.ObserveSnapshot(snapshot);
            selectedConsole.ObserveSnapshot(snapshot);

            Assert.AreEqual(3, wholeTest.Count, "The whole-test scope also counts the sibling console's host.");
            Assert.AreEqual(2, selectedConsole.Count);
            snapshot.Add(33, Tuple.Create(30, "testhost.x86"));
            selectedConsole.ObserveSnapshot(snapshot);
            Assert.AreEqual(3, selectedConsole.Count, "An extra host owned by the selected console must still be counted.");
        }

        [TestMethod]
        public void SelectedConsoleCountsItsDirectWorkersRatherThanTransitiveParentIds()
        {
            var snapshot = new Dictionary<int, Tuple<int, string>>
            {
                { 11000, Tuple.Create(7784, "testhost.x86") },
                { 4408, Tuple.Create(7784, "testhost.x86") },
                { 5956, Tuple.Create(11000, "testhost.x86") },
            };
            var allDescendants = new NumberOfProcessLaunchedUtility.ProcessTreeCounter(7784, "testhost.x86", new int[0]);
            var directWorkers = new NumberOfProcessLaunchedUtility.ProcessTreeCounter(7784, "testhost.x86", new int[0], includeDescendants: false);

            allDescendants.ObserveSnapshot(snapshot);
            directWorkers.ObserveSnapshot(snapshot);

            Assert.AreEqual(3, allDescendants.Count, "Reproduce the transitive count observed in CI.");
            Assert.AreEqual(2, directWorkers.Count);
            snapshot.Add(6000, Tuple.Create(7784, "testhost.x86"));
            directWorkers.ObserveSnapshot(snapshot);
            Assert.AreEqual(3, directWorkers.Count, "A third worker launched by the console must still fail the acceptance assertion.");
        }

        [TestMethod]
        public void FindsOnlyTheNewDirectConsoleChild()
        {
            var processId = NumberOfProcessLaunchedUtility.FindStartedChildProcessId(
                10,
                "vstest.console",
                new HashSet<int> { 20 },
                new Dictionary<int, Tuple<int, string>>
                {
                    { 20, Tuple.Create(10, "vstest.console") },
                    { 30, Tuple.Create(10, "VSTEST.CONSOLE") },
                    { 40, Tuple.Create(1, "vstest.console") },
                    { 50, Tuple.Create(10, "testhost") },
                    { 60, Tuple.Create(20, "vstest.console") },
                });

            Assert.AreEqual(30, processId);
        }

        [TestMethod]
        public void MissingOrAmbiguousConsoleOwnerFailsInsteadOfGuessing()
        {
            var processes = new Dictionary<int, Tuple<int, string>>();
            Assert.ThrowsException<InvalidOperationException>(() =>
                NumberOfProcessLaunchedUtility.FindStartedChildProcessId(10, "vstest.console", new HashSet<int>(), processes));

            processes.Add(20, Tuple.Create(10, "vstest.console"));
            processes.Add(30, Tuple.Create(10, "vstest.console"));
            Assert.ThrowsException<InvalidOperationException>(() =>
                NumberOfProcessLaunchedUtility.FindStartedChildProcessId(10, "vstest.console", new HashSet<int>(), processes));
        }
    }
}
