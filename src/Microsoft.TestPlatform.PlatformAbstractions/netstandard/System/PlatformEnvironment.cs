// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETSTANDARD

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

    /// <inheritdoc />
    public class PlatformEnvironment : IEnvironment
    {
        /// <inheritdoc />
        public PlatformArchitecture Architecture
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <inheritdoc />
        public PlatformOperatingSystem OperatingSystem
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <inheritdoc />
        public string OperatingSystemVersion
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <inheritdoc />
        public void Exit(int exitcode)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public int GetCurrentManagedThreadId()
        {
            throw new NotImplementedException();
        }
    }
}

#endif
