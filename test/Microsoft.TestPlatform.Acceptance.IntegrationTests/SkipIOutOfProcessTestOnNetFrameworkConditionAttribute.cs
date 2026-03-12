// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests;

public class SkipIOutOfProcessTestOnNetFrameworkConditionAttribute : ConditionBaseAttribute
{
    private readonly bool _include;

    public SkipIOutOfProcessTestOnNetFrameworkConditionAttribute() : base(ConditionMode.Include)
    {
        _include = !RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework");
        IgnoreMessage = "Test was skipped to avoid duplication of the same out-of-process tests between .NET and .NET Framework.";
    }

    public override string GroupName => "tfm";

    public override bool IsConditionMet => _include;
}
