// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage
{
    /* using System;
   using System.Collections.Generic;
   using System.Linq;
   using System.Text;
   using System.Windows;
   using System.Windows.Forms;
   using System.Windows.Forms.Integration;
   using System.Windows.Media;
   using System.Xml;
   using Microsoft.VisualStudio.TestTools.Execution;

   /// <summary>
   /// DynamicCoverageEditorPage
   /// </summary>
   [DataCollectorConfigurationEditorTypeUriAttribute("configurationeditor://Microsoft/CodeCoverageEditor/2.0")]
   public partial class DynamicCoverageEditorPage : UserControl, IDataCollectorConfigurationEditor
   {
       private ConfigEditorLayoutManager layoutManager;
       private XmlDocument ownerDoc;
       private Dictionary<IDynamicCoverageSettings, IDynamicCoverageSettingsControl> settingControls;
       private DataCollectorSettings dataCollectorSetting;

       internal const string RootName = "CodeCoverage";
       private const string Configuration = "Configuration";

       /// <summary>
       /// Constructor
       /// </summary>
       public DynamicCoverageEditorPage()
       {
           InitializeComponent();
       }

       /// <summary>
       /// Initialize the DynamicCoverageEditorPage
       /// </summary>
       /// <param name="serviceProvider">Service Provider</param>
       /// <param name="settings">Data Collector Settings</param>
       public void Initialize(IServiceProvider serviceProvider, DataCollectorSettings settings)
       {
           this.AutoSize = true;
           this.BorderStyle = BorderStyle.None;
           this.Padding = new Padding(3);

           dataCollectorSetting = settings;

           InitializeSettings(false);

           ProjectHelper.Initialize(serviceProvider);

           System.Windows.Controls.TabControl tabControl = new System.Windows.Controls.TabControl();

           System.Windows.Controls.TabItem tabItem;
           List<IDynamicCoverageSettings> settingList = settingControls.Keys.ToList();
           foreach (var setting in settingList)
           {
               IDynamicCoverageSettingsControl control = DynamicCoverageSettingsControlFactory.CreateControl(setting);
               if (control != null)
               {
                   tabItem = new System.Windows.Controls.TabItem();
                   tabItem.Content = control;
                   tabItem.Header = DynamicCoverageSettingsControlFactory.GetTabHeader(setting);
                   tabControl.Items.Add(tabItem);
                   settingControls[setting] = control;
               }
           }

           ElementHost host = new ElementHost();
           layoutManager = new ConfigEditorLayoutManager(host, tabControl);
           this.Controls.Add(layoutManager.RootPanel);
       }

       /// <summary>
       /// Reset all settings to default
       /// </summary>
       public void ResetToAgentDefaults()
       {
           InitializeSettings(true);

           foreach (var control in settingControls.Values)
           {
               if (control != null)
               {
                   control.UpdateUI();
               }
           }
       }

       /// <summary>
       /// Save current setting changes
       /// </summary>
       /// <returns>Data Collector Settings</returns>
       public DataCollectorSettings SaveData()
       {
           foreach (var control in settingControls.Values)
           {
               if (control != null)
               {
                   control.ApplyChanges();
               }
           }

           XmlElement configXml = ownerDoc.CreateElement(Configuration);
           XmlElement vanguardConfig = ownerDoc.CreateElement(RootName);
           configXml.AppendChild(vanguardConfig);

           foreach (var setting in settingControls.Keys)
           {
               foreach (XmlElement element in setting.ToXml())
               {
                   vanguardConfig.AppendChild(element);
               }
           }
           dataCollectorSetting.Configuration = configXml;

           return dataCollectorSetting;
       }

       /// <summary>
       /// Verify data
       /// </summary>
       /// <returns></returns>
       public bool VerifyData()
       {
           return true;
       }

       /// <summary>
       /// Initialize setting
       /// </summary>
       /// <param name="useDefault">whether to use default setting when initializing</param>
       private void InitializeSettings(bool useDefault)
       {
           XmlElement configElement;
           if (useDefault)
           {
               configElement = dataCollectorSetting.DefaultConfiguration[RootName];
           }
           else
           {
               configElement = dataCollectorSetting.Configuration[RootName];
           }

           if (configElement == null)
           {
               throw new XmlException(ConfigurationEditorUIResource.InvalidConfig);
           }

           ownerDoc = configElement.OwnerDocument;
           if (settingControls == null)
           {
               settingControls = new Dictionary<IDynamicCoverageSettings, IDynamicCoverageSettingsControl>();
               settingControls.Add(new DynamicCoverageModuleSettings(configElement), null);
               settingControls.Add(new DynamicCoverageAdvancedSettings(configElement), null);
               settingControls.Add(new DynamicCoverageReadOnlySettings(configElement), null);
           }
           else
           {
               foreach (var setting in settingControls.Keys)
               {
                   setting.LoadFromXml(configElement);
               }
           }
       }
   }

   /// <summary>
   /// ConfigEditorLayoutManager that layout the editor page
   /// </summary>
   internal class ConfigEditorLayoutManager
   {
       private FrameworkElement rootElement;
       private System.Windows.Forms.Panel panel;

       /// <summary>
       /// Root panel
       /// </summary>
       public System.Windows.Forms.Panel RootPanel
       {
           get { return panel; }
       }

       /// <summary>
       /// Constructor
       /// </summary>
       /// <param name="host">ElementHost</param>
       /// <param name="root">FrameworkElement</param>
       public ConfigEditorLayoutManager(ElementHost host, FrameworkElement root)
       {
           rootElement = root;
           panel = new System.Windows.Forms.Panel();
           panel.BorderStyle = BorderStyle.None;
           panel.Dock = DockStyle.Fill;
           panel.SizeChanged += PanelSizeChanged;
           panel.Controls.Add(host);
           panel.BorderStyle = BorderStyle.FixedSingle;
           host.Dock = DockStyle.Fill;
           host.Child = rootElement;

           rootElement.Width = panel.Width;
           rootElement.Height = panel.Height;
       }

       private void PanelSizeChanged(object sender, EventArgs e)
       {
           // Winforms uses pixels for measurements. WPF uses a device independant format that (at normal DPI settings) maps 1:1 for pixels
           // so to make this look right under high DPI settings we need to use the following matix to transform the winforms coordinates.
           Matrix m =
          PresentationSource.FromVisual(System.Windows.Application.Current.MainWindow).CompositionTarget.TransformToDevice;
           double dx = m.M11;
           double dy = m.M22;

           rootElement.Width = panel.Width / dx;
           rootElement.Height = panel.Height / dy;
       }
   } */
}