// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This type physically lives in the CrossPlatEngine project but keeps the CommunicationUtilities
// namespace to match TestRequestHandler, which it augments. Do not move the namespace/file; see
// TestRequestHandler.cs for the rationale (shipped public API compatibility).
namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

internal enum PathConversionDirection
{
    Receive,
    Send
}
