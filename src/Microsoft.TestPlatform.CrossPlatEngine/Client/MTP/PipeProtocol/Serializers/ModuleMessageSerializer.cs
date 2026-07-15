// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// This file is vendored from https://github.com/Youssef1313/MTPSharding (YTest.MTP.PipeProtocol),
// used under the MIT license with the author's permission. See THIRD-PARTY-NOTICES.txt.
#nullable enable

using System.IO;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP.PipeProtocol;

internal sealed class ModuleMessageSerializer : BaseSerializer, INamedPipeSerializer
{
    public int Id => ModuleFieldsId.MessagesSerializerId;

    public object Deserialize(Stream stream)
    {
        string modulePath = ReadString(stream);
        string projectPath = ReadString(stream);
        string targetFramework = ReadString(stream);
        string isTestingPlatformApplication = ReadString(stream);
        return new ModuleMessage(modulePath.Trim(), projectPath.Trim(), targetFramework.Trim(), isTestingPlatformApplication.Trim());
    }

    public void Serialize(object objectToSerialize, Stream stream)
    {
        WriteString(stream, ((ModuleMessage)objectToSerialize).DllOrExePath);
        WriteString(stream, ((ModuleMessage)objectToSerialize).ProjectPath);
        WriteString(stream, ((ModuleMessage)objectToSerialize).TargetFramework);
        WriteString(stream, ((ModuleMessage)objectToSerialize).IsTestingPlatformApplication);
    }
}
