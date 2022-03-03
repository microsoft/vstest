// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.ArtifactProcessing;

internal class TestArtifacts
{
    public TestArtifacts(string testSession!!, Artifact[] artifacts!!)
    {
        TestSession = testSession;
        Artifacts = artifacts;
    }

    public Artifact[] Artifacts { get; set; }

    public string TestSession { get; }
}

internal class Artifact
{
    public Artifact(string fileName!!, ArtifactType type)
    {
        FileName = fileName;
        Type = type;
    }

    public string FileName { get; }
    public ArtifactType Type { get; }
}

internal enum ArtifactType
{
    ExecutionComplete,
    Runsettings
}
