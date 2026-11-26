// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Build.Framework;

// MSBuild recognizes this attribute by full type name, allowing the task assembly
// to opt into multithreaded execution while targeting an older Microsoft.Build.Framework.
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class MSBuildMultiThreadableTaskAttribute : Attribute
{
}
