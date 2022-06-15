// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Utilities;

internal static class DiscoveredSourceHelper
{
    public static List<DiscoveredSource> ToDiscoveredSources(IEnumerable<string> sources)
    {
        return sources.Select(source => new DiscoveredSource { Source = source }).ToList();
    }
}
