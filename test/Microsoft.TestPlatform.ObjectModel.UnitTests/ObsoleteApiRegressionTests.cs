// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.ObjectModel.UnitTests;

/// <summary>
/// Guards the deprecation state of the ObjectModel members that were escalated to compile-time
/// errors for https://github.com/microsoft/vstest/issues/16415.
///
/// Both members stay in the assembly, so this change is source breaking only: already-compiled
/// assemblies keep loading and running unchanged. Only new source references are blocked.
/// </summary>
[TestClass]
public class ObsoleteApiRegressionTests
{
    // Some test methods below are themselves marked [Obsolete], because that is the only mechanism that
    // suppresses an error-level obsoletion. #pragma warning disable, NoWarn and .editorconfig severity
    // overrides do not apply to obsoletions declared with error: true.

    [TestMethod]
    [Obsolete("IDataCollectorAttachments is obsolete, but we want to test its deprecation state.")]
    public void IDataCollectorAttachmentsIsObsoleteAsError()
    {
        var obsolete = typeof(IDataCollectorAttachments).GetCustomAttribute<ObsoleteAttribute>();

        Assert.IsNotNull(obsolete);
        Assert.IsTrue(obsolete.IsError, "IDataCollectorAttachments must stay obsolete as an error.");
        Assert.Contains(nameof(IDataCollectorAttachmentProcessor), obsolete.Message!);
    }

    [TestMethod]
    public void IDataCollectorAttachmentsIsStillPresentForBinaryCompatibility()
    {
        // Deliberately looked up by name rather than with typeof/nameof, so that this test does not
        // create a source reference to the interface and can assert on it without being [Obsolete] itself.
        var type = typeof(AttachmentSet).Assembly
            .GetType("Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection.IDataCollectorAttachments");

        Assert.IsNotNull(type, "The interface must stay in the assembly so that existing compiled data collectors still load.");
        Assert.IsTrue(type.IsInterface);
        Assert.IsTrue(type.IsPublic);
        Assert.IsNotNull(type.GetMethod("GetExtensionUri"));
        Assert.IsNotNull(type.GetMethod("HandleDataCollectionAttachmentSets"));
    }

    [TestMethod]
    [Obsolete("TargetFrameworkVersion is obsolete, but we want to test its deprecation state.")]
    public void RunConfigurationTargetFrameworkVersionIsObsoleteAsError()
    {
        var property = typeof(RunConfiguration).GetProperty(nameof(RunConfiguration.TargetFrameworkVersion));

        Assert.IsNotNull(property);
        var obsolete = property.GetCustomAttribute<ObsoleteAttribute>();

        Assert.IsNotNull(obsolete);
        Assert.IsTrue(obsolete.IsError, "RunConfiguration.TargetFrameworkVersion must stay obsolete as an error.");
        Assert.Contains(nameof(RunConfiguration.TargetFramework), obsolete.Message!);
    }

    [TestMethod]
    public void RunConfigurationTargetFrameworkVersionStillRoundTripsThroughReflection()
    {
        // Assemblies compiled against an older ObjectModel still call these accessors, so the shim must keep
        // working. Looked up by name so that this test needs no [Obsolete] marker of its own.
        var runConfiguration = new RunConfiguration();
        var property = typeof(RunConfiguration).GetProperty("TargetFrameworkVersion")!;

        property.SetValue(runConfiguration, FrameworkVersion.Framework45);

        Assert.AreEqual(Framework.FromString("Framework45")!.Name, runConfiguration.TargetFramework!.Name);
        Assert.AreEqual(FrameworkVersion.Framework45, (FrameworkVersion)property.GetValue(runConfiguration)!);
    }

    [TestMethod]
    public void TargetFrameworkVersionRunSettingsElementIsUnaffected()
    {
        // Only the CLR property is source-blocked, the runsettings element keeps working and keeps its name.
        var settingsXml =
            """
            <?xml version="1.0" encoding="utf-8"?>
            <RunSettings>
              <RunConfiguration>
                <TargetFrameworkVersion>Framework45</TargetFrameworkVersion>
              </RunConfiguration>
            </RunSettings>
            """;

        var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(settingsXml);

        Assert.AreEqual(Framework.FromString("Framework45")!.Name, runConfiguration.TargetFramework!.Name);
        Assert.Contains("<TargetFrameworkVersion>", runConfiguration.ToXml().OuterXml);
    }
}
