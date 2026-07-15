// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// This file is vendored from https://github.com/Youssef1313/MTPSharding (YTest.MTP.PipeProtocol),
// used under the MIT license with the author's permission. See THIRD-PARTY-NOTICES.txt.
#nullable enable

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.MTP.PipeProtocol;

internal static class VoidResponseFieldsId
{
    public const int MessagesSerializerId = 0;
}

internal static class CommandLineOptionMessagesFieldsId
{
    public const int MessagesSerializerId = 3;

    public const ushort ModulePath = 1;
    public const ushort CommandLineOptionMessageList = 2;
}

internal static class CommandLineOptionMessageFieldsId
{
    public const ushort Name = 1;
    public const ushort Description = 2;
    public const ushort IsHidden = 3;
    public const ushort IsBuiltIn = 4;
}

internal static class ModuleFieldsId
{
    public const int MessagesSerializerId = 4;
}

internal static class DiscoveredTestMessagesFieldsId
{
    public const int MessagesSerializerId = 5;

    public const ushort ExecutionId = 1;
    public const ushort InstanceId = 2;
    public const ushort DiscoveredTestMessageList = 3;
}

internal static class DiscoveredTestMessageFieldsId
{
    public const ushort Uid = 1;
    public const ushort DisplayName = 2;
    public const ushort FilePath = 3;
    public const ushort LineNumber = 4;
    public const ushort Namespace = 5;
    public const ushort TypeName = 6;
    public const ushort MethodName = 7;
    public const ushort Traits = 8;
    public const ushort ParameterTypeFullNames = 9;
}

internal static class TraitMessageFieldsId
{
    public const ushort Key = 1;
    public const ushort Value = 2;
}

internal static class TestResultMessagesFieldsId
{
    public const int MessagesSerializerId = 6;

    public const ushort ExecutionId = 1;
    public const ushort InstanceId = 2;
    public const ushort SuccessfulTestMessageList = 3;
    public const ushort FailedTestMessageList = 4;
}

internal static class SuccessfulTestResultMessageFieldsId
{
    public const ushort Uid = 1;
    public const ushort DisplayName = 2;
    public const ushort State = 3;
    public const ushort Duration = 4;
    public const ushort Reason = 5;
    public const ushort StandardOutput = 6;
    public const ushort ErrorOutput = 7;
    public const ushort SessionUid = 8;
}

internal static class FailedTestResultMessageFieldsId
{
    public const ushort Uid = 1;
    public const ushort DisplayName = 2;
    public const ushort State = 3;
    public const ushort Duration = 4;
    public const ushort Reason = 5;
    public const ushort ExceptionMessageList = 6;
    public const ushort StandardOutput = 7;
    public const ushort ErrorOutput = 8;
    public const ushort SessionUid = 9;
}

internal static class ExceptionMessageFieldsId
{
    public const ushort ErrorMessage = 1;
    public const ushort ErrorType = 2;
    public const ushort StackTrace = 3;
}

internal static class FileArtifactMessagesFieldsId
{
    public const int MessagesSerializerId = 7;

    public const ushort ExecutionId = 1;
    public const ushort InstanceId = 2;
    public const ushort FileArtifactMessageList = 3;
}

internal static class FileArtifactMessageFieldsId
{
    public const ushort FullPath = 1;
    public const ushort DisplayName = 2;
    public const ushort Description = 3;
    public const ushort TestUid = 4;
    public const ushort TestDisplayName = 5;
    public const ushort SessionUid = 6;
}

internal static class TestSessionEventFieldsId
{
    public const int MessagesSerializerId = 8;

    public const ushort SessionType = 1;
    public const ushort SessionUid = 2;
    public const ushort ExecutionId = 3;
}

internal static class HandshakeMessageFieldsId
{
    public const int MessagesSerializerId = 9;
}
