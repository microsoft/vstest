// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel;

public class SourceDetail
{
    public string? Source { get; internal set; }
    public Architecture Architecture { get; internal set; }
    public Framework? Framework { get; internal set; }

    /// <summary>
    /// How this source should be hosted and communicated with. Defaults to <see cref="ExecutionPreference.Default"/>
    /// (vstest testhost protocol). Set to <see cref="ExecutionPreference.MicrosoftTestingPlatform"/> when the
    /// source is detected to be a Microsoft.Testing.Platform application.
    /// </summary>
    public ExecutionPreference ExecutionPreference { get; internal set; }
}
