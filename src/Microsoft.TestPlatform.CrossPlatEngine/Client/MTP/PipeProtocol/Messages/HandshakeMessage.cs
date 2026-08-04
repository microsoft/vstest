// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// This file is vendored from https://github.com/Youssef1313/MTPSharding (YTest.MTP.PipeProtocol),
// used under the MIT license with the author's permission. See THIRD-PARTY-NOTICES.txt.
#nullable enable

using System.Collections.Generic;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP.PipeProtocol;

internal sealed record HandshakeMessage(Dictionary<byte, string> Properties) : IRequest, IResponse;
