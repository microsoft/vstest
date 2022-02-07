// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Versioning;

using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

#pragma warning disable IDE1006 // Naming Styles
namespace vstest.ProgrammerTests.CommandLine.Fakes;

internal class FakeAssemblyMetadataProvider : IAssemblyMetadataProvider
{
    private readonly FakeFileHelper _fakeFileHelper;

    public FakeAssemblyMetadataProvider(FakeFileHelper fakeFileHelper)
    {
        _fakeFileHelper = fakeFileHelper;
    }

    public Architecture GetArchitecture(string filePath)
    {
        var file = (FakeDllFile)_fakeFileHelper.Files.Single(f => f.Path == filePath);
        return file.Architecture;
    }

    public FrameworkName GetFrameWork(string filePath)
    {
        var file = (FakeDllFile)_fakeFileHelper.Files.Single(f => f.Path == filePath);
        return file.FrameworkName;
    }
}
