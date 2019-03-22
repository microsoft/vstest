// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Internal
{
    internal class NoOpProgressIndicator : IProgressIndicator
    {
        /// <inheritdoc />
        public void Start()
        {
        }

        /// <inheritdoc />
        public void Pause()
        {
        }

        /// <inheritdoc />
        public void Stop()
        {
        }
    }
}
