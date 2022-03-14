// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.TestUtilities;

public class TesthostInfo : DllInfo
{
    public TesthostInfo(string versionType, string? version, string path)
        : base(name: "Testhost", propertyName: "VSTestConsole", versionType, version, path)
    {
    }
}
