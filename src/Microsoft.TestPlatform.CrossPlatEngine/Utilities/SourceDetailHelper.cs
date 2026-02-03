// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Xml;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Utilities;

internal static class SourceDetailHelper
{
    internal static string UpdateRunSettingsFromSourceDetail(string runSettings, SourceDetail sourceDetail)
    {
        using var stream = new StringReader(runSettings);
        using var reader = XmlReader.Create(stream, XmlRunSettingsUtilities.ReaderSettings);
        var document = new XmlDocument();
        document.Load(reader);
        var navigator = document.CreateNavigator();

        InferRunSettingsHelper.UpdateTargetFramework(document, sourceDetail.Framework!.ToString(), overwrite: true);
        InferRunSettingsHelper.UpdateTargetPlatform(document, sourceDetail.Architecture.ToString(), overwrite: true);

        if (sourceDetail.ExecutionPreference == ExecutionPreference.RunAsExe)
        {
            // When running as executable it makes no sense to pass the test app config to the exe to load it, this happens automatically
            // when we run the exe thanks to runtime.
            InferRunSettingsHelper.UpdateExecutionPreference(document, sourceDetail.ExecutionPreference!.ToString(), overwrite: true);
            InferRunSettingsHelper.UpdateDisableAppDomain(document, true.ToString(), overwrite: true);
        }

        var updatedRunSettings = navigator!.OuterXml;
        return updatedRunSettings;
    }
}
