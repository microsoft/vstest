// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.CommandLine;
using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests;

[TestClass]
public class DotnetRootEnvironmentNormalizerTests
{
    private readonly Mock<IEnvironmentVariableHelper> _environmentVariableHelper = new();

    [TestMethod]
    public void NormalizeShouldDoNothingWhenSdkProvidedVsTestDotnetRootPath()
    {
        // When the SDK launches us (dotnet test) it sets VSTEST_DOTNET_ROOT_PATH and manages DOTNET_ROOT_<ARCH>
        // itself, so we must not touch the environment.
        _environmentVariableHelper.Setup(x => x.GetEnvironmentVariable("VSTEST_DOTNET_ROOT_PATH")).Returns(@"C:\dotnet");
        _environmentVariableHelper.Setup(x => x.GetEnvironmentVariable("DOTNET_ROOT")).Returns(@"C:\dotnet");

        var normalizer = new TestableDotnetRootEnvironmentNormalizer(_environmentVariableHelper.Object, isWindows: true, architecture: "X64");

        normalizer.NormalizeDotnetRootForChildProcesses();

        _environmentVariableHelper.Verify(x => x.SetEnvironmentVariable(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void NormalizeShouldDoNothingOnNonWindows()
    {
        _environmentVariableHelper.Setup(x => x.GetEnvironmentVariable("DOTNET_ROOT")).Returns(@"/usr/share/dotnet");

        var normalizer = new TestableDotnetRootEnvironmentNormalizer(_environmentVariableHelper.Object, isWindows: false, architecture: "X64");

        normalizer.NormalizeDotnetRootForChildProcesses();

        _environmentVariableHelper.Verify(x => x.SetEnvironmentVariable(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void NormalizeShouldDoNothingWhenNoDotnetRoot()
    {
        var normalizer = new TestableDotnetRootEnvironmentNormalizer(_environmentVariableHelper.Object, isWindows: true, architecture: "X64");

        normalizer.NormalizeDotnetRootForChildProcesses();

        _environmentVariableHelper.Verify(x => x.SetEnvironmentVariable(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void NormalizeShouldDoNothingWhenArchitectureCannotBeDetermined()
    {
        _environmentVariableHelper.Setup(x => x.GetEnvironmentVariable("DOTNET_ROOT")).Returns(@"C:\dotnet");

        var normalizer = new TestableDotnetRootEnvironmentNormalizer(_environmentVariableHelper.Object, isWindows: true, architecture: null);

        normalizer.NormalizeDotnetRootForChildProcesses();

        _environmentVariableHelper.Verify(x => x.SetEnvironmentVariable(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void NormalizeShouldPromoteArchitectureLessDotnetRootAndClearIt()
    {
        _environmentVariableHelper.Setup(x => x.GetEnvironmentVariable("DOTNET_ROOT")).Returns(@"C:\Program Files\dotnet");

        var normalizer = new TestableDotnetRootEnvironmentNormalizer(_environmentVariableHelper.Object, isWindows: true, architecture: "X64");

        normalizer.NormalizeDotnetRootForChildProcesses();

        // The architecture-less DOTNET_ROOT is promoted to the architecture it actually points at...
        _environmentVariableHelper.Verify(x => x.SetEnvironmentVariable("DOTNET_ROOT_X64", @"C:\Program Files\dotnet"), Times.Once);
        // ...and the ambiguous architecture-less DOTNET_ROOT is cleared.
        _environmentVariableHelper.Verify(x => x.SetEnvironmentVariable("DOTNET_ROOT", string.Empty), Times.Once);
    }

    [TestMethod]
    public void NormalizeShouldNotOverrideExistingArchitectureSpecificDotnetRootButStillClearArchitectureLessOne()
    {
        _environmentVariableHelper.Setup(x => x.GetEnvironmentVariable("DOTNET_ROOT")).Returns(@"C:\Program Files\dotnet");
        _environmentVariableHelper.Setup(x => x.GetEnvironmentVariable("DOTNET_ROOT_X64")).Returns(@"D:\private-x64-dotnet");

        var normalizer = new TestableDotnetRootEnvironmentNormalizer(_environmentVariableHelper.Object, isWindows: true, architecture: "X64");

        normalizer.NormalizeDotnetRootForChildProcesses();

        // The already-set architecture specific value is respected (not overridden)...
        _environmentVariableHelper.Verify(x => x.SetEnvironmentVariable("DOTNET_ROOT_X64", It.IsAny<string>()), Times.Never);
        // ...but the ambiguous architecture-less DOTNET_ROOT is still cleared.
        _environmentVariableHelper.Verify(x => x.SetEnvironmentVariable("DOTNET_ROOT", string.Empty), Times.Once);
    }

    private sealed class TestableDotnetRootEnvironmentNormalizer : DotnetRootEnvironmentNormalizer
    {
        private readonly bool _isWindows;
        private readonly string? _architecture;

        public TestableDotnetRootEnvironmentNormalizer(IEnvironmentVariableHelper environmentVariableHelper, bool isWindows, string? architecture)
            : base(environmentVariableHelper)
        {
            _isWindows = isWindows;
            _architecture = architecture;
        }

        internal override bool IsWindows => _isWindows;

        internal override string? GetExecutableArchitecture(string executablePath) => _architecture;
    }
}
