// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.TestPlatform.TestUtilities;

[Serializable]
public class NetTestSdkInfo : DllInfo
{
    public NetTestSdkInfo()
    {
        Name = "Testhost";
        PropertyName = "VSTestConsole";
    }
}
