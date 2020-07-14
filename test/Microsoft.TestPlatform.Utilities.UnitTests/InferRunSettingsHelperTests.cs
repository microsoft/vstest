// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Utilities.UnitTests
{
	using System;
	using System.Xml;

	using Microsoft.VisualStudio.TestPlatform.ObjectModel;
	using OMResources = Microsoft.VisualStudio.TestPlatform.ObjectModel.Resources.CommonResources;
	using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
	using Microsoft.VisualStudio.TestPlatform.Utilities;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using MSTest.TestFramework.AssertExtensions;
	using System.Collections.Generic;
	using System.Linq;
	using System.Globalization;
	using System.Text;

	[TestClass]
	public class InferRunSettingsHelperTests
	{
		private IDictionary<string, Architecture> sourceArchitectures;
		private IDictionary<string, Framework> sourceFrameworks;
		private readonly Framework frameworkNet45 = Framework.FromString(".NETFramework,Version=4.5");
		private readonly Framework frameworkNet46 = Framework.FromString(".NETFramework,Version=4.6");
		private readonly Framework frameworkNet47 = Framework.FromString(".NETFramework,Version=4.7");

		public InferRunSettingsHelperTests()
		{
			sourceArchitectures = new Dictionary<string, Architecture>();
			sourceFrameworks = new Dictionary<string, Framework>();
		}

		[TestMethod]
		public void UpdateRunSettingsShouldThrowIfRunSettingsNodeDoesNotExist()
		{
			var settings = @"<RandomSettings></RandomSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			Action action = () => InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X86, Framework.DefaultFramework, "temp");

			Assert.That.Throws<XmlException>(action)
						.WithMessage(string.Format("An error occurred while loading the settings.  Error: {0}.",
											"Could not find 'RunSettings' node."));
		}

		[TestMethod]
		public void UpdateRunSettingsShouldThrowIfPlatformNodeIsInvalid()
		{
			var settings = @"<RunSettings><RunConfiguration><TargetPlatform>foo</TargetPlatform></RunConfiguration></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			Action action = () => InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X86, Framework.DefaultFramework, "temp");

			Assert.That.Throws<XmlException>(action)
						.WithMessage(string.Format("An error occurred while loading the settings.  Error: {0}.",
										string.Format("Invalid setting '{0}'. Invalid value '{1}' specified for '{2}'",
											"RunConfiguration",
											"foo",
											"TargetPlatform")));
		}

		[TestMethod]
		public void UpdateRunSettingsShouldThrowIfFrameworkNodeIsInvalid()
		{
			var settings = @"<RunSettings><RunConfiguration><TargetFrameworkVersion>foo</TargetFrameworkVersion></RunConfiguration></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			Action action = () => InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X86, Framework.DefaultFramework, "temp");

			Assert.That.Throws<XmlException>(action)
						.WithMessage(string.Format("An error occurred while loading the settings.  Error: {0}.",
										string.Format("Invalid setting '{0}'. Invalid value '{1}' specified for '{2}'",
										"RunConfiguration",
										"foo",
										"TargetFrameworkVersion")));
		}

		[TestMethod]
		public void UpdateRunSettingsShouldUpdateWithPlatformSettings()
		{
			var settings = @"<RunSettings></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X86, Framework.DefaultFramework, "temp");

			var xml = xmlDocument.OuterXml;

			StringAssert.Contains(xml, "<TargetPlatform>X86</TargetPlatform>");
		}

		[TestMethod]
		public void UpdateRunSettingsShouldUpdateWithFrameworkSettings()
		{
			var settings = @"<RunSettings></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X86, Framework.DefaultFramework, "temp");

			var xml = xmlDocument.OuterXml;

			StringAssert.Contains(xml, $"<TargetFrameworkVersion>{Framework.DefaultFramework.Name}</TargetFrameworkVersion>");
		}

		[TestMethod]
		public void UpdateRunSettingsShouldUpdateWithResultsDirectorySettings()
		{
			var settings = @"<RunSettings></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X86, Framework.DefaultFramework, "temp");

			var xml = xmlDocument.OuterXml;

			StringAssert.Contains(xml, "<ResultsDirectory>temp</ResultsDirectory>");
		}

		[TestMethod]
		public void UpdateRunSettingsShouldNotUpdatePlatformIfRunSettingsAlreadyHasIt()
		{
			var settings = @"<RunSettings><RunConfiguration><TargetPlatform>X86</TargetPlatform></RunConfiguration></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X64, Framework.DefaultFramework, "temp");

			var xml = xmlDocument.OuterXml;

			StringAssert.Contains(xml, "<TargetPlatform>X86</TargetPlatform>");
		}

		[TestMethod]
		public void UpdateRunSettingsShouldNotUpdateFrameworkIfRunSettingsAlreadyHasIt()
		{
			var settings = @"<RunSettings><RunConfiguration><TargetFrameworkVersion>Framework40</TargetFrameworkVersion></RunConfiguration></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X64, Framework.DefaultFramework, "temp");

			var xml = xmlDocument.OuterXml;

			StringAssert.Contains(xml, "<TargetFrameworkVersion>.NETFramework,Version=v4.0</TargetFrameworkVersion>");
		}
		//TargetFrameworkMoniker

		[TestMethod]
		public void UpdateRunSettingsShouldAllowTargetFrameworkMonikerValue()
		{

			var settings = @"<RunSettings><RunConfiguration><TargetFrameworkVersion>.NETFramework,Version=v4.0</TargetFrameworkVersion></RunConfiguration></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X64, Framework.DefaultFramework, "temp");

			var xml = xmlDocument.OuterXml;

			StringAssert.Contains(xml, "<TargetFrameworkVersion>.NETFramework,Version=v4.0</TargetFrameworkVersion>");
		}

		[TestMethod]
		public void UpdateRunSettingsShouldNotUpdateResultsDirectoryIfRunSettingsAlreadyHasIt()
		{
			var settings = @"<RunSettings><RunConfiguration><ResultsDirectory>someplace</ResultsDirectory></RunConfiguration></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X64, Framework.DefaultFramework, "temp");

			var xml = xmlDocument.OuterXml;

			StringAssert.Contains(xml, "<ResultsDirectory>someplace</ResultsDirectory>");
		}

		[TestMethod]
		public void UpdateRunSettingsShouldNotUpdatePlatformOrFrameworkOrResultsDirectoryIfRunSettingsAlreadyHasIt()
		{
			var settings = @"<RunSettings><RunConfiguration><TargetPlatform>X86</TargetPlatform><TargetFrameworkVersion>Framework40</TargetFrameworkVersion><ResultsDirectory>someplace</ResultsDirectory></RunConfiguration></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X64, Framework.DefaultFramework, "temp");

			var xml = xmlDocument.OuterXml;

			StringAssert.Contains(xml, "<TargetPlatform>X86</TargetPlatform>");
			StringAssert.Contains(xml, "<TargetFrameworkVersion>Framework40</TargetFrameworkVersion>");
			StringAssert.Contains(xml, "<ResultsDirectory>someplace</ResultsDirectory>");
		}

		[TestMethod]
		public void UpdateRunSettingsWithAnEmptyRunSettingsShouldAddValuesSpecifiedInRunConfiguration()
		{
			var settings = @"<RunSettings></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X64, Framework.DefaultFramework, "temp");

			var xml = xmlDocument.OuterXml;

			StringAssert.Contains(xml, "<TargetPlatform>X64</TargetPlatform>");
			StringAssert.Contains(xml, $"<TargetFrameworkVersion>{Framework.DefaultFramework.Name}</TargetFrameworkVersion>");
			StringAssert.Contains(xml, "<ResultsDirectory>temp</ResultsDirectory>");
		}

		[TestMethod]
		public void UpdateRunSettingsShouldReturnBackACompleteRunSettings()
		{
			var settings = @"<RunSettings></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.X64, Framework.DefaultFramework, "temp");

			var xml = xmlDocument.OuterXml;
			var expectedRunSettings = string.Format("<RunSettings><RunConfiguration><ResultsDirectory>temp</ResultsDirectory><TargetPlatform>X64</TargetPlatform><TargetFrameworkVersion>{0}</TargetFrameworkVersion></RunConfiguration></RunSettings>", Framework.DefaultFramework.Name);

			Assert.AreEqual(expectedRunSettings, xml);
		}

		[TestMethod]
		public void UpdateRunSettingsShouldThrowIfArchitectureSetIsIncompatibleWithCurrentSystemArchitecture()
		{
			var settings = @"<RunSettings></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			Action action = () => InferRunSettingsHelper.UpdateRunSettingsWithUserProvidedSwitches(xmlDocument, Architecture.ARM, Framework.DefaultFramework, "temp");

			Assert.That.Throws<SettingsException>(action)
				.WithMessage(string.Format(
						"Incompatible Target platform settings '{0}' with system architecture '{1}'.",
						"ARM",
						XmlRunSettingsUtilities.OSArchitecture.ToString()));
		}

		[TestMethod]
		public void UpdateDesignModeOrCsiShouldNotModifyXmlIfNodeIsAlreadyPresent()
		{
			var settings = @"<RunSettings><RunConfiguration><DesignMode>False</DesignMode><CollectSourceInformation>False</CollectSourceInformation></RunConfiguration></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			InferRunSettingsHelper.UpdateDesignMode(xmlDocument, true);
			InferRunSettingsHelper.UpdateCollectSourceInformation(xmlDocument, true);

			Assert.AreEqual("False", this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/DesignMode"));
			Assert.AreEqual("False", this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/CollectSourceInformation"));
		}

		[DataTestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public void UpdateDesignModeOrCsiShouldModifyXmlToValueProvided(bool val)
		{
			var settings = @"<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			InferRunSettingsHelper.UpdateDesignMode(xmlDocument, val);
			InferRunSettingsHelper.UpdateCollectSourceInformation(xmlDocument, val);

			Assert.AreEqual(val.ToString(), this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/DesignMode"));
			Assert.AreEqual(val.ToString(), this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/CollectSourceInformation"));
		}

		[TestMethod]
		public void MakeRunsettingsCompatibleShouldDeleteNewlyAddedRunConfigurationNode()
		{
			var settings = @"<RunSettings><RunConfiguration><DesignMode>False</DesignMode><CollectSourceInformation>False</CollectSourceInformation></RunConfiguration></RunSettings>";

			var result = InferRunSettingsHelper.MakeRunsettingsCompatible(settings);

			Assert.IsTrue(result.IndexOf("DesignMode", StringComparison.OrdinalIgnoreCase) < 0);
		}

		[TestMethod]
		public void MakeRunsettingsCompatibleShouldNotDeleteOldRunConfigurationNode()
		{
			var settings = @"<RunSettings>
								<RunConfiguration>
									<DesignMode>False</DesignMode>
									<CollectSourceInformation>False</CollectSourceInformation>
									<TargetPlatform>x86</TargetPlatform>
									<TargetFrameworkVersion>net46</TargetFrameworkVersion>
									<TestAdaptersPaths>dummypath</TestAdaptersPaths>
									<ResultsDirectory>dummypath</ResultsDirectory>
									<SolutionDirectory>dummypath</SolutionDirectory>
									<MaxCpuCount>2</MaxCpuCount>
									<DisableParallelization>False</DisableParallelization>
									<DisableAppDomain>False</DisableAppDomain>
								</RunConfiguration>
							</RunSettings>";

			var result = InferRunSettingsHelper.MakeRunsettingsCompatible(settings);

			Assert.IsTrue(result.IndexOf("TargetPlatform", StringComparison.OrdinalIgnoreCase) > 0);
			Assert.IsTrue(result.IndexOf("TargetFrameworkVersion", StringComparison.OrdinalIgnoreCase) > 0);
			Assert.IsTrue(result.IndexOf("TestAdaptersPaths", StringComparison.OrdinalIgnoreCase) > 0);
			Assert.IsTrue(result.IndexOf("ResultsDirectory", StringComparison.OrdinalIgnoreCase) > 0);
			Assert.IsTrue(result.IndexOf("SolutionDirectory", StringComparison.OrdinalIgnoreCase) > 0);
			Assert.IsTrue(result.IndexOf("MaxCpuCount", StringComparison.OrdinalIgnoreCase) > 0);
			Assert.IsTrue(result.IndexOf("DisableParallelization", StringComparison.OrdinalIgnoreCase) > 0);
			Assert.IsTrue(result.IndexOf("DisableAppDomain", StringComparison.OrdinalIgnoreCase) > 0);
		}

		[TestMethod]
		public void UpdateTargetDeviceValueFromOldMsTestSettings()
		{
			var settings = @"<RunSettings>
								<RunConfiguration>
									<MaxCpuCount>2</MaxCpuCount>
									<DisableParallelization>False</DisableParallelization>
									<DisableAppDomain>False</DisableAppDomain>
								</RunConfiguration>
								<MSPhoneTest>
								  <TargetDevice>169.254.193.190</TargetDevice>
								</MSPhoneTest>
							</RunSettings>";

			var xmlDocument = this.GetXmlDocument(settings);

			var result = InferRunSettingsHelper.TryGetDeviceXml(xmlDocument.CreateNavigator(), out string deviceXml);
			Assert.IsTrue(result);

			InferRunSettingsHelper.UpdateTargetDevice(xmlDocument, deviceXml);
			Assert.AreEqual(deviceXml.ToString(), this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/TargetDevice"));
		}

		[TestMethod]
		public void UpdateTargetPlatformShouldNotModifyXmlIfNodeIsAlreadyPresentForOverwriteFalse()
		{
			var settings = @"<RunSettings><RunConfiguration><TargetPlatform>x86</TargetPlatform></RunConfiguration></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			InferRunSettingsHelper.UpdateTargetPlatform(xmlDocument, "X64", overwrite: false);

			Assert.AreEqual("x86", this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/TargetPlatform"));
		}

		[TestMethod]
		public void UpdateTargetPlatformShouldModifyXmlIfNodeIsAlreadyPresentForOverwriteTrue()
		{
			var settings = @"<RunSettings><RunConfiguration><TargetPlatform>x86</TargetPlatform></RunConfiguration></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			InferRunSettingsHelper.UpdateTargetPlatform(xmlDocument, "X64", overwrite: true);

			Assert.AreEqual("X64", this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/TargetPlatform"));
		}

		[TestMethod]
		public void UpdateTargetPlatformShouldAddPlatformXmlNodeIfNotPresent()
		{
			var settings = @"<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			InferRunSettingsHelper.UpdateTargetPlatform(xmlDocument, "X64");

			Assert.AreEqual("X64", this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/TargetPlatform"));
		}

		[TestMethod]
		public void UpdateTargetFrameworkShouldNotModifyXmlIfNodeIsAlreadyPresentForOverwriteFalse()
		{
			var settings = @"<RunSettings><RunConfiguration><TargetFrameworkVersion>.NETFramework,Version=v4.5</TargetFrameworkVersion></RunConfiguration></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			InferRunSettingsHelper.UpdateTargetFramework(xmlDocument, ".NETCoreApp,Version=v1.0", overwrite: false);

			Assert.AreEqual(".NETFramework,Version=v4.5", this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/TargetFrameworkVersion"));
		}

		[TestMethod]
		public void UpdateTargetFrameworkShouldModifyXmlIfNodeIsAlreadyPresentForOverwriteTrue()
		{
			var settings = @"<RunSettings><RunConfiguration><TargetFrameworkVersion>.NETFramework,Version=v4.5</TargetFrameworkVersion></RunConfiguration></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			InferRunSettingsHelper.UpdateTargetFramework(xmlDocument, ".NETCoreApp,Version=v1.0", overwrite: true);

			Assert.AreEqual(".NETCoreApp,Version=v1.0", this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/TargetFrameworkVersion"));
		}

		[TestMethod]
		public void UpdateTargetFrameworkShouldAddFrameworkXmlNodeIfNotPresent()
		{
			var settings = @"<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";
			var xmlDocument = this.GetXmlDocument(settings);

			InferRunSettingsHelper.UpdateTargetFramework(xmlDocument, ".NETCoreApp,Version=v1.0");

			Assert.AreEqual(".NETCoreApp,Version=v1.0", this.GetValueOf(xmlDocument, "/RunSettings/RunConfiguration/TargetFrameworkVersion"));
		}

		[TestMethod]
		public void FilterCompatibleSourcesShouldIdentifyIncomaptiableSourcesAndConstructWarningMessage()
		{
			#region Arrange
			sourceArchitectures["AnyCPU1net46.dll"] = Architecture.AnyCPU;
			sourceArchitectures["x64net47.exe"] = Architecture.X64;
			sourceArchitectures["x86net45.dll"] = Architecture.X86;

			sourceFrameworks["AnyCPU1net46.dll"] = frameworkNet46;
			sourceFrameworks["x64net47.exe"] = frameworkNet47;
			sourceFrameworks["x86net45.dll"] = frameworkNet45;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine();
			sb.AppendLine(GetSourceIncompatibleMessage("AnyCPU1net46.dll"));
			sb.AppendLine(GetSourceIncompatibleMessage("x64net47.exe"));
			sb.AppendLine(GetSourceIncompatibleMessage("x86net45.dll"));

			var expected = string.Format(CultureInfo.CurrentCulture, OMResources.DisplayChosenSettings, frameworkNet47, Constants.DefaultPlatform, sb.ToString(), @"http://go.microsoft.com/fwlink/?LinkID=236877&clcid=0x409");
			#endregion

			string warningMessage = string.Empty;
			var compatibleSources = InferRunSettingsHelper.FilterCompatibleSources(Constants.DefaultPlatform, Constants.DefaultPlatform, frameworkNet47, sourceArchitectures, sourceFrameworks, out warningMessage);

			// None of the DLLs passed are compatible to the chosen settings
			Assert.AreEqual(0, compatibleSources.Count());
			Assert.AreEqual(expected, warningMessage);
		}

		[TestMethod]
		public void FilterCompatibleSourcesShouldIdentifyCompatibleSources()
		{
			sourceArchitectures["x64net45.exe"] = Architecture.X64;
			sourceArchitectures["x86net45.dll"] = Architecture.X86;

			sourceFrameworks["x64net45.exe"] = frameworkNet45;
			sourceFrameworks["x86net45.dll"] = frameworkNet45;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine();
			sb.AppendLine(GetSourceIncompatibleMessage("x64net45.exe"));

			var expected = string.Format(CultureInfo.CurrentCulture, OMResources.DisplayChosenSettings, frameworkNet45, Constants.DefaultPlatform, sb.ToString(), @"http://go.microsoft.com/fwlink/?LinkID=236877&clcid=0x409");

			string warningMessage = string.Empty;
			var compatibleSources = InferRunSettingsHelper.FilterCompatibleSources(Constants.DefaultPlatform, Constants.DefaultPlatform, frameworkNet45, sourceArchitectures, sourceFrameworks, out warningMessage);

			// only "x86net45.dll" is the compatible source
			Assert.AreEqual(1, compatibleSources.Count());
			Assert.AreEqual(expected, warningMessage);
		}

		[TestMethod]
		public void FilterCompatibleSourcesShouldNotComposeWarningIfSettingsAreCorrect()
		{
			sourceArchitectures["x86net45.dll"] = Architecture.X86;
			sourceFrameworks["x86net45.dll"] = frameworkNet45;

			string warningMessage = string.Empty;
			var compatibleSources = InferRunSettingsHelper.FilterCompatibleSources(Constants.DefaultPlatform, Constants.DefaultPlatform, frameworkNet45, sourceArchitectures, sourceFrameworks, out warningMessage);

			// only "x86net45.dll" is the compatible source
			Assert.AreEqual(1, compatibleSources.Count());
			Assert.IsTrue(string.IsNullOrEmpty(warningMessage));
		}

		[TestMethod]
		public void FilterCompatibleSourcesShouldRetrunWarningMessageIfNoConflict()
		{
			sourceArchitectures["x64net45.exe"] = Architecture.X64;
			sourceFrameworks["x64net45.exe"] = frameworkNet45;

			string warningMessage = string.Empty;
			var compatibleSources = InferRunSettingsHelper.FilterCompatibleSources(Architecture.X64, Constants.DefaultPlatform, frameworkNet45, sourceArchitectures, sourceFrameworks, out warningMessage);

			Assert.IsTrue(string.IsNullOrEmpty(warningMessage));
		}

		[TestMethod]
		public void IsTestSettingsEnabledShouldReturnTrueIfRunsettingsHasTestSettings()
		{
			string runsettingsString = @"<RunSettings>
										<MSTest>
											<SettingsFile>C:\temp.testsettings</SettingsFile>
											<ForcedLegacyMode>true</ForcedLegacyMode>
										</MSTest>
									</RunSettings>";

			Assert.IsTrue(InferRunSettingsHelper.IsTestSettingsEnabled(runsettingsString));
		}

		[TestMethod]
		public void IsTestSettingsEnabledShouldReturnFalseIfRunsettingsDoesnotHaveTestSettings()
		{
			string runsettingsString = @"<RunSettings>
										<MSTest>
											<ForcedLegacyMode>true</ForcedLegacyMode>
										</MSTest>
									</RunSettings>";

			Assert.IsFalse(InferRunSettingsHelper.IsTestSettingsEnabled(runsettingsString));
		}

		[TestMethod]
		public void TryGetLegacySettingsForRunSettingsWithoutLegacySettingsShouldReturnFalse()
		{
			string runSettingsXml = @"<RunSettings>
									  </RunSettings>";

			Assert.IsFalse(InferRunSettingsHelper.TryGetLegacySettingElements(runSettingsXml, out Dictionary<string, string> legacySettings));
		}

		[TestMethod]
		public void TryGetLegacySettingsForRunSettingsWithInvalidLegacySettingsShouldReturnFalse()
		{
			string runSettingsXml = @"<RunSettings>
										<LegacySettings>
											<Foo>
										</LegacySettings>
									  </RunSettings>";

			Assert.IsFalse(InferRunSettingsHelper.TryGetLegacySettingElements(runSettingsXml, out Dictionary<string, string> legacySettings));
		}

		[TestMethod]
		public void TryGetLegacySettingsForRunSettingsWithEmptyLegacySettingsShouldReturnTrueAndEmptyListForLegacySettingElements()
		{
			string runSettingsXml = @"<RunSettings>
										<LegacySettings>
										</LegacySettings>
									  </RunSettings>";

			Assert.IsTrue(InferRunSettingsHelper.TryGetLegacySettingElements(runSettingsXml, out Dictionary<string, string> legacySettings));
			Assert.AreEqual(0, legacySettings.Count);
		}

		[TestMethod]
		public void TryGetLegacySettingsForRunSettingsWithValidLegacySettingsShouldReturnTrueAndListForLegacySettingElements()
		{
			string runSettingsXml = @"<RunSettings>
									   <LegacySettings>
											<Deployment enabled=""true"" deploySatelliteAssemblies=""true"" >
												<DeploymentItem filename="".\test.txt"" />
											</Deployment>
											<Scripts setupScript="".\setup.bat"" cleanupScript="".\cleanup.bat"" />
											<Execution hostProcessPlatform=""MSIL"" parallelTestCount=""4"">
												<Timeouts testTimeout=""120"" />
												<TestTypeSpecific>
													<UnitTestRunConfig>
														<AssemblyResolution />
													</UnitTestRunConfig>
												</TestTypeSpecific>
												<Hosts />
											</Execution>
									   </LegacySettings>
									  </RunSettings>";

			var expectedElements = "Deployment, Scripts, Execution, AssemblyResolution, Timeouts, Hosts";
			var expectedDeploymentAttributes = "enabled, deploySatelliteAssemblies";
			var expectedExecutionAttributes = "hostProcessPlatform, parallelTestCount";

			Assert.IsTrue(InferRunSettingsHelper.TryGetLegacySettingElements(runSettingsXml, out Dictionary<string, string> legacySettings));
			Assert.AreEqual(3, legacySettings.Count, "count does not match");
			Assert.AreEqual(expectedElements, legacySettings["Elements"]);
			Assert.AreEqual(expectedDeploymentAttributes, legacySettings["DeploymentAttributes"]);
			Assert.AreEqual(expectedExecutionAttributes, legacySettings["ExecutionAttributes"]);
		}

		[TestMethod]
		public void GetEnvironmentVariablesWithValidValuesInRunSettingsShouldReturnValidDictionary()
		{
			string runSettingsXml = @"<RunSettings>
									   <RunConfiguration>
										  <EnvironmentVariables>
											 <RANDOM_PATH>C:\temp</RANDOM_PATH>
											 <RANDOM_PATH2>C:\temp2</RANDOM_PATH2>
										  </EnvironmentVariables>
									   </RunConfiguration>
									  </RunSettings>";

			var envVars = InferRunSettingsHelper.GetEnvironmentVariables(runSettingsXml);

            Assert.AreEqual(2, envVars.Count);
            Assert.AreEqual(@"C:\temp", envVars["RANDOM_PATH"]);
            Assert.AreEqual(@"C:\temp2", envVars["RANDOM_PATH2"]);
        }

		[TestMethod]
		public void GetEnvironmentVariablesWithDuplicateEnvValuesInRunSettingsShouldReturnValidDictionary()
		{
			string runSettingsXml = @"<RunSettings>
									   <RunConfiguration>
										  <EnvironmentVariables>
											 <RANDOM_PATH>C:\temp</RANDOM_PATH>
											 <RANDOM_PATH>C:\temp2</RANDOM_PATH>
										  </EnvironmentVariables>
									   </RunConfiguration>
									  </RunSettings>";

			var envVars = InferRunSettingsHelper.GetEnvironmentVariables(runSettingsXml);

            Assert.AreEqual(1, envVars.Count);
            Assert.AreEqual(@"C:\temp", envVars["RANDOM_PATH"]);
        }

		[TestMethod]
		public void GetEnvironmentVariablesWithEmptyVariablesInRunSettingsShouldReturnEmptyDictionary()
		{
			string runSettingsXml = @"<RunSettings>
									   <RunConfiguration>
										 <EnvironmentVariables>
										 </EnvironmentVariables>
									   </RunConfiguration>
									  </RunSettings>";

			var envVars = InferRunSettingsHelper.GetEnvironmentVariables(runSettingsXml);
			Assert.AreEqual(0, envVars.Count);
		}

		[TestMethod]
		public void GetEnvironmentVariablesWithInvalidValuesInRunSettingsShouldReturnNull()
		{
			string runSettingsXml = @"<RunSettings>
									   <RunConfiguration>
										 <EnvironmentVariables>
											<Foo>
										 </EnvironmentVariables>
									   </RunConfiguration>
									  </RunSettings>";

			var envVars = InferRunSettingsHelper.GetEnvironmentVariables(runSettingsXml);
			Assert.IsNull(envVars);
		}

		[TestMethod]
		public void GetEnvironmentVariablesWithoutEnvVarNodeInRunSettingsShouldReturnNull()
		{
			string runSettingsXml = @"<RunSettings>
									   <RunConfiguration>
									   </RunConfiguration>
									  </RunSettings>";

			var envVars = InferRunSettingsHelper.GetEnvironmentVariables(runSettingsXml);
			Assert.IsNull(envVars);
		}

		#region RunSettingsIncompatibeWithTestSettings Tests

		[TestMethod]
		public void RunSettingsWithCodeCoverageAndInlineTestSettingsXml()
		{
			// Setup
			var runSettingsWithCodeCoverageAndInlineTestSettingsXml = @"
					<RunSettings>
					  <RunConfiguration>
						<TargetFrameworkVersion>Framework45</TargetFrameworkVersion>
						<ResultsDirectory>C:\TestProject1\TestResults</ResultsDirectory>
						<SolutionDirectory>C:\TestProject1\</SolutionDirectory>
						<TargetPlatform>X86</TargetPlatform>
					  </RunConfiguration>
					  <MSTest>
						<SettingsFile>C:\TestProject1\TestSettings1.testsettings</SettingsFile>
						<ForcedLegacyMode>true</ForcedLegacyMode>
						<IgnoreTestImpact>true</IgnoreTestImpact>
					  </MSTest>
					  <DataCollectionRunSettings>
						<DataCollectors>
						  <DataCollector friendlyName=""Code Coverage"" uri=""datacollector://Microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""DynamicCoverageDataCollector"">
							<Configuration>
							  <CoverageFileName>DevBox 2018-01-07 20_42_30.coverage</CoverageFileName>
							  <CodeCoverage>
							  </CodeCoverage>
							</Configuration>
						  </DataCollector>
						</DataCollectors>
					  </DataCollectionRunSettings>
					</RunSettings>";

			// Act and validate
			Assert.IsFalse(InferRunSettingsHelper.AreRunSettingsCollectorsInCompatibleWithTestSettings(
				runSettingsWithCodeCoverageAndInlineTestSettingsXml), "Invalid response");
			Assert.IsTrue(InferRunSettingsHelper.AreRunSettingsCollectorsInCompatibleWithTestSettings(
				ConvertOutOfProcToInProcDataCollectionSettings(runSettingsWithCodeCoverageAndInlineTestSettingsXml)), "Invalid response");
		}

		[TestMethod]
		public void RunSettingsWithFakesAndCodeCoverageAndInlineTestSettingsXml()
		{
			var runSettingsWithFakesAndCodeCoverageAndInlineTestSettingsXml = @"
				<RunSettings>
				  <RunConfiguration>
					<TargetFrameworkVersion>Framework45</TargetFrameworkVersion>
					<ResultsDirectory>C:\TestProject1\TestResults</ResultsDirectory>
					<SolutionDirectory>C:\TestProject1\</SolutionDirectory>
					<TargetPlatform>X86</TargetPlatform>
				  </RunConfiguration>
				  <MSTest>
					<SettingsFile>C:\TestProject1\TestSettings1.testsettings</SettingsFile>
					<ForcedLegacyMode>true</ForcedLegacyMode>
					<IgnoreTestImpact>true</IgnoreTestImpact>
				  </MSTest>
				  <DataCollectionRunSettings>
					<DataCollectors>
					  <DataCollector friendlyName=""Code Coverage"" uri=""datacollector://Microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""DynamicCoverageDataCollector"">
					  </DataCollector>
					  <DataCollector friendlyName=""UnitTestIsolation"" uri=""datacollector://Microsoft/unittestisolation/1.0"" assemblyQualifiedName=""DynamicCoverageDataCollector"">
					  </DataCollector>
					</DataCollectors>
				  </DataCollectionRunSettings>
				</RunSettings>";

			// Act and validate
			Assert.IsFalse(InferRunSettingsHelper.AreRunSettingsCollectorsInCompatibleWithTestSettings(
				runSettingsWithFakesAndCodeCoverageAndInlineTestSettingsXml), "Invalid response");
			Assert.IsTrue(InferRunSettingsHelper.AreRunSettingsCollectorsInCompatibleWithTestSettings(
				ConvertOutOfProcToInProcDataCollectionSettings(runSettingsWithFakesAndCodeCoverageAndInlineTestSettingsXml)), "Invalid response");
		}

		[TestMethod]
		public void RunSettingsWithEnabledAndDisabledCollectorAndNoEmbeddedTestSettingsXml()
		{
			var runSettingsWithEnabledAndDisabledCollectorAndInlineTestSettingsXml = @"
				<RunSettings>
					<RunConfiguration>
						<TargetFrameworkVersion>Framework45</TargetFrameworkVersion>
						<ResultsDirectory>C:\TestProject1\TestResults</ResultsDirectory>
						<SolutionDirectory>C:\TestProject1\</SolutionDirectory>
						<TargetPlatform>X86</TargetPlatform>
					</RunConfiguration>
					<DataCollectionRunSettings>
					<DataCollectors>
						<DataCollector friendlyName=""Video"" uri=""datacollector://Microsoft/Video/2.0"" assemblyQualifiedName=""VideoCollector"">
						</DataCollector>
					</DataCollectors>
					<DataCollectors>
						<DataCollector friendlyName=""EventLog"" uri=""datacollector://Microsoft/Log/2.0"" enabled=""false"" assemblyQualifiedName=""LogCollector"">
						</DataCollector>
					</DataCollectors>
					</DataCollectionRunSettings>
				</RunSettings>";

			// Act and validate
			Assert.IsFalse(InferRunSettingsHelper.AreRunSettingsCollectorsInCompatibleWithTestSettings(
				runSettingsWithEnabledAndDisabledCollectorAndInlineTestSettingsXml), "Invalid response");
			Assert.IsFalse(InferRunSettingsHelper.AreRunSettingsCollectorsInCompatibleWithTestSettings(
				ConvertOutOfProcToInProcDataCollectionSettings(runSettingsWithEnabledAndDisabledCollectorAndInlineTestSettingsXml)), "Invalid response");
		}

		[TestMethod]
		public void RunSettingsWithEnabledAndDisabledCollectorAndInlineTestSettingsXml()
		{
			var runSettingsWithEnabledAndDisabledCollectorAndInlineTestSettingsXml = @"
				<RunSettings>
					<RunConfiguration>
						<TargetFrameworkVersion>Framework45</TargetFrameworkVersion>
						<ResultsDirectory>C:\TestProject1\TestResults</ResultsDirectory>
						<SolutionDirectory>C:\TestProject1\</SolutionDirectory>
						<TargetPlatform>X86</TargetPlatform>
					</RunConfiguration>
					<MSTest>
						<SettingsFile>C:\TestProject1\TestSettings1.testsettings</SettingsFile>
						<ForcedLegacyMode>true</ForcedLegacyMode>
						<IgnoreTestImpact>true</IgnoreTestImpact>
					</MSTest>
					<DataCollectionRunSettings>
					<DataCollectors>
						<DataCollector friendlyName=""Video"" uri=""datacollector://Microsoft/Video/2.0"" assemblyQualifiedName=""VideoCollector"">
						</DataCollector>
					</DataCollectors>
					<DataCollectors>
						<DataCollector friendlyName=""EventLog"" uri=""datacollector://Microsoft/Log/2.0"" enabled=""false"" assemblyQualifiedName=""LogCollector"">
						</DataCollector>
					</DataCollectors>
					</DataCollectionRunSettings>
				</RunSettings>";

			// Act and validate
			Assert.IsTrue(InferRunSettingsHelper.AreRunSettingsCollectorsInCompatibleWithTestSettings(
				runSettingsWithEnabledAndDisabledCollectorAndInlineTestSettingsXml), "Invalid response");
			Assert.IsTrue(InferRunSettingsHelper.AreRunSettingsCollectorsInCompatibleWithTestSettings(
				ConvertOutOfProcToInProcDataCollectionSettings(runSettingsWithEnabledAndDisabledCollectorAndInlineTestSettingsXml)), "Invalid response");
		}

		[TestMethod]
		public void RunSettingsWithDisabledCollectionSettingsAndInlineTestSettingsXml()
		{
			var runSettingsWithDisabledCollectionSettingsAndInlineTestSettingsXml = @"
				<RunSettings>
				  <RunConfiguration>
					<TargetFrameworkVersion>Framework45</TargetFrameworkVersion>
					<ResultsDirectory>C:\TestProject1\TestResults</ResultsDirectory>
					<SolutionDirectory>C:\TestProject1\</SolutionDirectory>
					<TargetPlatform>X86</TargetPlatform>
				  </RunConfiguration>
				  <MSTest>
					<SettingsFile>C:\TestProject1\TestSettings1.testsettings</SettingsFile>
					<ForcedLegacyMode>true</ForcedLegacyMode>
					<IgnoreTestImpact>true</IgnoreTestImpact>
				  </MSTest>
				  <DataCollectionRunSettings>
					<DataCollectors>
					  <DataCollector friendlyName=""Video"" uri=""datacollector://Microsoft/Video/2.0"" enabled=""false"" assemblyQualifiedName=""VideoCollector"">
					  </DataCollector>
					</DataCollectors>
					<DataCollectors>
					  <DataCollector friendlyName=""EventLog"" uri=""datacollector://Microsoft/Log/2.0"" enabled=""false"" assemblyQualifiedName=""LogCollector"">
					  </DataCollector>
					</DataCollectors>
				  </DataCollectionRunSettings>
				</RunSettings>";

			// Act and validate
			Assert.IsFalse(InferRunSettingsHelper.AreRunSettingsCollectorsInCompatibleWithTestSettings(
				runSettingsWithDisabledCollectionSettingsAndInlineTestSettingsXml), "Invalid response");
			Assert.IsFalse(InferRunSettingsHelper.AreRunSettingsCollectorsInCompatibleWithTestSettings(
				ConvertOutOfProcToInProcDataCollectionSettings(runSettingsWithDisabledCollectionSettingsAndInlineTestSettingsXml)), "Invalid response");
		}

		#endregion

		#region Private Methods

		private string GetSourceIncompatibleMessage(string source)
		{
			return string.Format(CultureInfo.CurrentCulture, OMResources.SourceIncompatible, source, sourceFrameworks[source].Name, sourceArchitectures[source]);
		}

		private XmlDocument GetXmlDocument(string settingsXml)
		{
			var doc = new XmlDocument();
			doc.LoadXml(settingsXml);

			return doc;
		}

		private string GetValueOf(XmlDocument xmlDocument, string xpath)
		{
			return xmlDocument.SelectSingleNode(xpath).InnerText;
		}

		private string ConvertOutOfProcToInProcDataCollectionSettings(string settings)
		{
			return settings.Replace("DataCollectionRunSettings", "InProcDataCollectionRunSettings")
						   .Replace("<DataCollectors>", "<InProcDataCollectors>")
						   .Replace("</DataCollectors>", "</InProcDataCollectors>")
						   .Replace("<DataCollector ", "<InProcDataCollector ")
						   .Replace("</DataCollector>", "</InProcDataCollector>");
		}

		#endregion
	}
}
