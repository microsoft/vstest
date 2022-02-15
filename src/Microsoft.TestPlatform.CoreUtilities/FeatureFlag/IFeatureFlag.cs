// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.Utilities;

internal interface IFeatureFlag
{
    bool IsEnabled(string featureName);
}
