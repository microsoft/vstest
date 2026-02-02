// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel;


[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public class RunAsExeAttribute : Attribute
{
}

public enum ExecutionPreference
{
    Default,
    RunAsExe,
}
