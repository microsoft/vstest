// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.CommandLine.Internal;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Internal;

[TestClass]
public class ProgressIndicatorTests
{
    private readonly SteppableTimer _steppableTimer;
    private readonly ProgressIndicator _indicator;
    private readonly Mock<IOutput> _consoleOutput;
    private readonly Mock<IConsoleHelper> _consoleHelper;

    public ProgressIndicatorTests()
    {
        _consoleOutput = new Mock<IOutput>();
        _consoleHelper = new Mock<IConsoleHelper>();
        _consoleHelper.Setup(c => c.WindowWidth).Returns(100);
        _consoleHelper.Setup(c => c.CursorTop).Returns(20);
        _steppableTimer = new SteppableTimer();
        _indicator = new ProgressIndicator(_consoleOutput.Object, _consoleHelper.Object, _steppableTimer);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _indicator.Stop();
    }

    [TestMethod]
    public void StartShouldStartPrintingProgressMessage()
    {
        _indicator.Start();
        _consoleOutput.Verify(m => m.Write("Test run in progress.", OutputLevel.Information), Times.Once);
        Assert.IsTrue(_indicator.IsRunning);
    }

    [TestMethod]
    public void StartShouldShowProgressMessageAndDotForEveryTimeATimerFires()
    {
        _consoleHelper.Setup(c => c.CursorLeft).Returns(30);

        _indicator.Start();
        Assert.IsTrue(_indicator.IsRunning);
        _consoleOutput.Verify(m => m.Write("Test run in progress.", OutputLevel.Information), Times.Once);

        _steppableTimer.Step();

        _consoleOutput.Verify(m => m.Write(".", OutputLevel.Information), Times.Once);

        _steppableTimer.Step();

        _consoleOutput.Verify(m => m.Write(".", OutputLevel.Information), Times.Exactly(2));

        _indicator.Stop();
    }

    [TestMethod]
    public void PauseShouldClearTheStdOutMessage()
    {
        _indicator.Start();
        _indicator.Pause();

        Assert.IsFalse(_indicator.IsRunning);
        string clearMessage = new(' ', _consoleHelper.Object.WindowWidth);
        _consoleOutput.Verify(m => m.Write("Test run in progress.", OutputLevel.Information), Times.Once);
        _consoleOutput.Verify(m => m.Write(clearMessage, OutputLevel.Information), Times.Once);

        _consoleHelper.Verify(ch => ch.SetCursorPosition(0, 20), Times.Exactly(2));
    }

    [TestMethod]
    public void PauseStartAndStopShouldClearPrintProgressAndThenClearTheStdOutMessage()
    {
        _indicator.Start();
        _indicator.Pause();
        _indicator.Start();
        _indicator.Stop();

        Assert.IsFalse(_indicator.IsRunning);
        string clearMessage = new(' ', _consoleHelper.Object.WindowWidth);
        _consoleOutput.Verify(m => m.Write("Test run in progress.", OutputLevel.Information), Times.Exactly(2));
        _consoleOutput.Verify(m => m.Write(clearMessage, OutputLevel.Information), Times.Exactly(2));
        _consoleHelper.Verify(ch => ch.SetCursorPosition(0, 20), Times.Exactly(4));
    }

    [TestMethod]
    public void StopShouldClearTheStdOutMessage()
    {
        _indicator.Start();
        _indicator.Stop();

        Assert.IsFalse(_indicator.IsRunning);
        string clearMessage = new(' ', _consoleHelper.Object.WindowWidth);
        _consoleOutput.Verify(m => m.Write("Test run in progress.", OutputLevel.Information), Times.Once);
        _consoleOutput.Verify(m => m.Write(clearMessage, OutputLevel.Information), Times.Once);
        _consoleHelper.Verify(ch => ch.SetCursorPosition(0, 20), Times.Exactly(2));
    }

    [TestMethod]
    public void RepeatedPauseAndStartShouldNotDuplicateElapsedSubscription()
    {
        _consoleHelper.Setup(c => c.CursorLeft).Returns(30);

        // Mirrors ConsoleLogger.TestMessageHandler, which calls Pause() then Start()
        // around every logged message. Elapsed should be subscribed exactly once
        // (in the constructor), regardless of how many Pause/Start cycles occur.
        _indicator.Start();
        for (var i = 0; i < 20; i++)
        {
            _indicator.Pause();
            _indicator.Start();
        }

        _steppableTimer.Step();

        // A single Step() should produce exactly one "." write, not N writes for N
        // prior Start() calls.
        _consoleOutput.Verify(m => m.Write(".", OutputLevel.Information), Times.Once);
    }

    [TestMethod]
    public void ClearShouldNotThrowWhenStartPositionIsNegative()
    {
        // Simulates a race where Timer_Elapsed computes a cursor position based on
        // stale state right as Pause()/Start() resets it, or a console window that
        // has shrunk since the last read of CursorLeft/WindowWidth.
        _consoleHelper.Setup(c => c.CursorLeft).Returns(1);

        _indicator.Start();

        // CursorLeft (1) - 3 would be negative; this must not throw.
        _steppableTimer.Step();
        _steppableTimer.Step();
        _steppableTimer.Step();
    }
}
