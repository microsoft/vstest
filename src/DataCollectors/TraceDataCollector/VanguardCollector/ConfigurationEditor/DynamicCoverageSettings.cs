// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;

#pragma warning disable SA1649 // File name must match first type name

    /// <summary>
    /// Interface for all DynamicCoverageSetting
    /// </summary>
    internal interface IDynamicCoverageSettings
#pragma warning restore SA1649 // File name must match first type name
    {
        /// <summary>
        /// Convert DynamicCoverageSettings to xml
        /// </summary>
        /// <returns>setting xml elements</returns>
        IEnumerable<XmlElement> ToXml();

        /// <summary>
        /// Load DynamicCoverageSettings from xml
        /// </summary>
        /// <param name="element">setting xml element</param>
        void LoadFromXml(XmlElement element);
    }

    internal static class Utility
    {
        /// <summary>
        /// Semicolon
        /// </summary>
        private const char Semicolon = ';';

        /// <summary>
        /// Quote
        /// </summary>
        private const char Quote = '"';

        /// <summary>
        /// XmlNode.SelectSingleNode and XmlNode.SelectNodes is too heavy when element has xmlns
        /// FindChildrenByName is used to replace SelectNodes when we need to select children by name
        /// </summary>
        /// <param name="parent">parent node</param>
        /// <param name="name">element name to search</param>
        /// <param name="verify">verify</param>
        /// <returns>xml elements</returns>
        internal static IEnumerable<XmlNode> FindChildrenByName(XmlNode parent, string name, bool verify = false)
        {
            foreach (XmlNode node in parent.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                if (verify && !node.Name.Equals(name))
                {
                    throw new XmlException(ConfigurationEditorUIResource.InvalidConfig);
                }

                if (node.Name.Equals(name))
                {
                    yield return node;
                }
            }
        }

        /// <summary>
        /// Check whether a list of string contains(ignore case) a given string
        /// </summary>
        /// <param name="list">list of string</param>
        /// <param name="str">string to search</param>
        /// <returns>whether contains the given string</returns>
        internal static bool ContainsStringIgnoreCase(IEnumerable<string> list, string str)
        {
            foreach (string item in list)
            {
                if (string.Compare(item, str, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Parase a semicolon separated string to a list of strings.
        /// Semicolon separated string: a string that contains multiple substrings. Substrings are separated by semicolons.
        /// If a substring itself contains semicolons, it should be surrounded by quotes. Quotes are not allowed in the substrings. They are always treated as special character.
        /// </summary>
        /// <param name="str">Semicolon separated string</param>
        /// <returns>List of strings</returns>
        internal static IEnumerable<string> FromSemicolonSeparatedString(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

            bool insideQuote = false;
            int begin = 0;
            for (int i = 0; i <= str.Length; i++)
            {
                if (i == str.Length || str[i] == Semicolon)
                {
                    if (!insideQuote)
                    {
                        string item = str.Substring(begin, i - begin).Trim().Replace("\"", string.Empty);
                        if (!string.IsNullOrEmpty(item))
                        {
                            yield return item;
                        }

                        begin = i + 1;
                    }
                }
                else if (str[i] == Quote)
                {
                    insideQuote = !insideQuote;
                }
            }

            if (insideQuote)
            {
                throw new ArgumentException(null, "str");
            }
        }

        /// <summary>
        /// Convert a list of strings to a string separated by semicolon
        /// </summary>
        /// <param name="list">List of strings</param>
        /// <returns>Semicolon separated string</returns>
        internal static string ToSemicolonSeparatedString(IEnumerable<string> list)
        {
            if (list == null)
            {
                throw new ArgumentNullException("list");
            }

            StringBuilder builder = new StringBuilder();
            bool first = true;
            foreach (var str in list)
            {
                if (string.IsNullOrEmpty(str))
                {
                    continue;
                }

                if (!first)
                {
                    builder.Append(Semicolon);
                }

                first = false;
                if (str.Contains(Quote))
                {
                    // Quotes are not allowed in input strings.
                    throw new ArgumentException(null, "list");
                }

                if (str.Contains(Semicolon))
                {
                    // If string contains semicolon, surround it with quotes.
                    builder.Append(Quote).Append(str).Append(Quote);
                }
                else
                {
                    builder.Append(str);
                }
            }

            return builder.ToString();
        }
    }

#pragma warning disable SA1402 // File may only contain a single class

    /// <summary>
    /// DynamicCoverageAdvancedSettings
    /// </summary>
    internal class DynamicCoverageAdvancedSettings : IDynamicCoverageSettings
#pragma warning restore SA1402 // File may only contain a single class
    {
        internal const string AllowedUserListName = "AllowedUsers";
        internal const string AllowedUserElementName = "User";
        internal const string Everyone = "Everyone";

        private const string CollectAspDotNetStr = "CollectAspDotNet";
        private const string CollectChildProcessStr = "CollectFromChildProcesses";
        private const string UseVerifiableInstrumentationStr = "UseVerifiableInstrumentation";
        private const string AllowLowIntegrityProcessStr = "AllowLowIntegrityProcesses";

        // We do not want to show code coverage for modules which are auto-generated by the compiler
        // Modules generated by ASP.NET for web pages(.aspx) come under this category.
        private const string ExcludeCompilerAutoGeneratedModulesStr = "ExcludeCompilerAutoGeneratedModules";
        private const string SymbolPathListName = "SymbolSearchPaths";
        private const string SymbolPathElementName = "Path";

        private const string CompanyNameListName = "CompanyNames";
        private const string CompanyNameElementName = "CompanyName";
        private const string CompanyNameSample = @".*microsoft.*";
        private const string PublicKeyTokenListName = "PublicKeyTokens";
        private const string PublicKeyTokenElementName = "PublicKeyToken";
        private const string PublickeyTokenSample = "^1234$";

        private Dictionary<string, object> generalSettings = new Dictionary<string, object>
        {
            { CollectChildProcessStr, true },
            { UseVerifiableInstrumentationStr, true },
            { AllowLowIntegrityProcessStr, true },
            { ExcludeCompilerAutoGeneratedModulesStr, true },
        };

        private XmlDocument ownerDoc = new XmlDocument();

        internal DynamicCoverageAdvancedSettings(XmlElement settings)
        {
            this.SymbolPathList = new SimpleListSettings(settings, SymbolPathListName, SymbolPathElementName);
            this.AllowedUserList = new SimpleListSettings(settings, AllowedUserListName, AllowedUserElementName);

            this.CompanyNameList = new FilteringListSettings(
                settings,
                CompanyNameListName,
                CompanyNameElementName,
                FilteringListSettings.FilterType.Exclusion,
                CompanyNameSample,
                true);

            this.PublicKeyTokenList = new FilteringListSettings(
                settings,
                PublicKeyTokenListName,
                PublicKeyTokenElementName,
                FilteringListSettings.FilterType.Exclusion,
                PublickeyTokenSample,
                true);

            LoadGeneralSettingFromXml(settings, ref this.generalSettings, ref this.ownerDoc);
        }

        /// <summary>
        /// Gets or sets a value indicating whether useVerifiableInstrumentation setting
        /// </summary>
        internal bool UseVerifiableInstrumentation
        {
            get { return GetSetting<bool>(UseVerifiableInstrumentationStr, this.generalSettings); }

            set { UpdateSetting(UseVerifiableInstrumentationStr, value, this.generalSettings); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether allowLowIntegrityProcess setting
        /// </summary>
        internal bool AllowLowIntegrityProcess
        {
            get { return GetSetting<bool>(AllowLowIntegrityProcessStr, this.generalSettings); }

            set { UpdateSetting(AllowLowIntegrityProcessStr, value, this.generalSettings); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether collectChildProcess setting
        /// </summary>
        internal bool CollectChildProcess
        {
            get { return GetSetting<bool>(CollectChildProcessStr, this.generalSettings); }

            set { UpdateSetting(CollectChildProcessStr, value, this.generalSettings); }
        }

        /// <summary>
        /// Gets allowed user setting
        /// </summary>
        internal SimpleListSettings AllowedUserList { get; private set; }

        /// <summary>
        /// Gets symbolPathList setting
        /// </summary>
        internal SimpleListSettings SymbolPathList { get; private set; }

        /// <summary>
        /// Gets or sets companyNameList setting
        /// </summary>
        internal FilteringListSettings CompanyNameList { get; set; }

        /// <summary>
        /// Gets or sets publicKeyTokenList setting
        /// </summary>
        internal FilteringListSettings PublicKeyTokenList { get; set; }

        /// <summary>
        /// Load DynamicCoverageAdvancedSettings from xml
        /// </summary>
        /// <param name="settings">settings xml.</param>
        public void LoadFromXml(XmlElement settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException();
            }

            LoadGeneralSettingFromXml(settings, ref this.generalSettings, ref this.ownerDoc);
            this.SymbolPathList.LoadFromXml(settings);
            this.AllowedUserList.LoadFromXml(settings);
            this.CompanyNameList.LoadFromXml(settings);
            this.PublicKeyTokenList.LoadFromXml(settings);
        }

        /// <summary>
        /// Convert DynamicCoverageAdvancedSettings to xml
        /// </summary>
        /// <returns>xml</returns>
        public IEnumerable<XmlElement> ToXml()
        {
            foreach (string settingName in this.generalSettings.Keys)
            {
                XmlElement settingElement = this.ownerDoc.CreateElement(settingName);
                settingElement.InnerText = this.generalSettings[settingName].ToString();
                yield return settingElement;
            }

            foreach (XmlElement element in this.SymbolPathList.ToXml())
            {
                yield return element;
            }

            foreach (XmlElement element in this.AllowedUserList.ToXml())
            {
                yield return element;
            }

            foreach (XmlElement element in this.CompanyNameList.ToXml())
            {
                yield return element;
            }

            foreach (XmlElement element in this.PublicKeyTokenList.ToXml())
            {
                yield return element;
            }
        }

        internal static void LoadGeneralSettingFromXml(
            XmlElement settings,
            ref Dictionary<string, object> generalSettings,
            ref XmlDocument ownerDoc)
        {
            ownerDoc = settings.OwnerDocument;

            XmlNode node = null;
            List<string> settingNames = generalSettings.Keys.ToList();
            foreach (string settingName in settingNames)
            {
                if ((node = settings[settingName]) != null)
                {
                    object setting = generalSettings[settingName];
                    if (setting is bool)
                    {
                        generalSettings[settingName] = bool.Parse(node.InnerText);
                    }
                    else
                    {
                        Debug.Fail(ConfigurationEditorUIResource.InvalidSettingType);
                    }
                }
            }
        }

        internal static T GetSetting<T>(string settingName, Dictionary<string, object> generalSettings)
        {
            object setting = null;
            if (generalSettings.TryGetValue(settingName, out setting))
            {
                return (T)setting;
            }

            return default(T);
        }

        internal static bool HasSetting(string settingName, Dictionary<string, object> generalSettings)
        {
            return generalSettings.ContainsKey(settingName);
        }

        internal static void UpdateSetting<T>(string settingName, T value, Dictionary<string, object> generalSettings)
        {
            generalSettings[settingName] = value;
        }
    }

#pragma warning disable SA1402 // File may only contain a single class

    /// <summary>
    /// DynamicCoverageModuleSettings
    /// </summary>
    internal class DynamicCoverageModuleSettings : IDynamicCoverageSettings
#pragma warning restore SA1402 // File may only contain a single class
    {
        internal const string EntryPointListName = "EntryPoints";
        internal const string EntryPointElementName = "EntryPoint";
        internal const string EntryPointSample = "sample.exe";
        internal const string ModulePathElementName = "ModulePath";

        private const string ModulePathListName = "ModulePaths";
        private const string ModulePathSample = @"*\VC\redist\*";
        private const string CollectAspDotNetStr = "CollectAspDotNet";

        private Dictionary<string, object> generalSettings =
            new Dictionary<string, object> { { CollectAspDotNetStr, false } };

        private XmlDocument ownerDoc = new XmlDocument();

        internal DynamicCoverageModuleSettings(XmlElement settings, bool loadAll = true)
        {
            if (loadAll)
            {
                this.ModulePathList = new FilteringListSettings(
                    settings,
                    ModulePathListName,
                    ModulePathElementName,
                    FilteringListSettings.FilterType.Inclusion,
                    ModulePathSample,
                    true);
                this.EntryPointList = new SimpleListSettings(settings, EntryPointListName, EntryPointElementName);
            }

            DynamicCoverageAdvancedSettings.LoadGeneralSettingFromXml(
                settings,
                ref this.generalSettings,
                ref this.ownerDoc);
        }

        /// <summary>
        /// Gets modulePathList setting
        /// </summary>
        internal FilteringListSettings ModulePathList { get; private set; }

        /// <summary>
        /// Gets entry point setting
        /// </summary>
        internal SimpleListSettings EntryPointList { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether collectAspDotNet setting
        /// </summary>
        internal bool CollectAspDotNet
        {
            get { return DynamicCoverageAdvancedSettings.GetSetting<bool>(CollectAspDotNetStr, this.generalSettings); }

            set { DynamicCoverageAdvancedSettings.UpdateSetting(CollectAspDotNetStr, value, this.generalSettings); }
        }

        /// <summary>
        /// Load ModulePublicKeyTokenList from xml
        /// </summary>
        /// <param name="settings">xml element for setting</param>
        public void LoadFromXml(XmlElement settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException();
            }

            DynamicCoverageAdvancedSettings.LoadGeneralSettingFromXml(
                settings,
                ref this.generalSettings,
                ref this.ownerDoc);
            this.ModulePathList.LoadFromXml(settings);
            this.EntryPointList.LoadFromXml(settings);
        }

        /// <summary>
        /// Convert DynamicCoverageModuleSettings to xml
        /// </summary>
        /// <returns>xml</returns>
        public IEnumerable<XmlElement> ToXml()
        {
            foreach (string settingName in this.generalSettings.Keys)
            {
                XmlElement settingElement = this.ownerDoc.CreateElement(settingName);
                settingElement.InnerText = this.generalSettings[settingName].ToString();
                yield return settingElement;
            }

            foreach (XmlElement element in this.ModulePathList.ToXml())
            {
                yield return element;
            }

            foreach (XmlElement element in this.EntryPointList.ToXml())
            {
                yield return element;
            }
        }
    }

#pragma warning disable SA1402 // File may only contain a single class

    /// <summary>
    /// DynamicCoverageReadOnlySettings
    /// </summary>
    internal class DynamicCoverageReadOnlySettings : IDynamicCoverageSettings
#pragma warning restore SA1402 // File may only contain a single class
    {
        private static readonly string[] FunctionSig = { "Functions", "Function" };
        private static readonly string[] FunctionAttr = { "Attributes", "Attribute" };
        private static readonly string[] Source = { "Sources", "Source" };
        private static readonly string[] Company = { "CompanyNames", "CompanyName" };
        private static readonly string[] ModulePKT = { "PublicKeyTokens", "PublicKeyToken" };
        private static readonly string[][] ListNames = { FunctionSig, FunctionAttr, Source };

        private static List<NameElementPair> readOnlySettings;

        /// <summary>
        /// Initializes static members of the <see cref="DynamicCoverageReadOnlySettings"/> class.
        /// Static constructor for DynamicCoverageReadOnlySettings
        /// </summary>
        static DynamicCoverageReadOnlySettings()
        {
            readOnlySettings = new List<NameElementPair>();
            foreach (string[] names in ListNames)
            {
                readOnlySettings.Add(new NameElementPair(names[0], names[1]));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicCoverageReadOnlySettings"/> class.
        /// DynamicCoverageReadOnlySettings Constructor
        /// </summary>
        /// <param name="settings">settings</param>
        internal DynamicCoverageReadOnlySettings(XmlElement settings)
        {
            this.LoadFromXml(settings);
        }

        /// <summary>
        /// Load list setting from xml
        /// </summary>
        /// <param name="settings">setting xml element</param>
        public void LoadFromXml(XmlElement settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException();
            }

            foreach (NameElementPair pair in readOnlySettings)
            {
                pair.Config = null;
                XmlElement configElement = settings[pair.ListName];
                if (configElement != null)
                {
                    FilteringListSettings.VerifyFilteringSetting(
                        configElement,
                        pair.ListName,
                        pair.ElementName,
                        FilteringListSettings.FilterType.Both);
                    pair.Config = configElement;
                }
            }
        }

        /// <summary>
        /// Convert DynamicCoverageReadOnlySettings to xml
        /// </summary>
        /// <returns>xml</returns>
        public IEnumerable<XmlElement> ToXml()
        {
            foreach (NameElementPair pair in readOnlySettings)
            {
                if (pair.Config != null)
                {
                    yield return pair.Config;
                }
            }
        }

        private class NameElementPair
        {
            internal NameElementPair(string listName, string elementName)
            {
                this.ListName = listName;
                this.ElementName = elementName;
            }

            internal string ElementName { get; private set; }

            internal string ListName { get; private set; }

            internal XmlElement Config { get; set; }
        }
    }

#pragma warning disable SA1402 // File may only contain a single class

    /// <inheritdoc />
    internal class SimpleListSettings : IDynamicCoverageSettings
#pragma warning restore SA1402 // File may only contain a single class
    {
        private XmlDocument ownerDoc;

        internal SimpleListSettings(XmlElement settings, string listName, string elementName)
        {
            this.ListName = listName;
            this.ElementName = elementName;
            this.List = new List<string>();

            this.LoadFromXml(settings);
        }

        /// <summary>
        /// Gets list name of this list setting
        /// </summary>
        internal string ListName { get; private set; }

        /// <summary>
        /// Gets element name of this list setting
        /// </summary>
        internal string ElementName { get; private set; }

        /// <summary>
        /// Gets content of this list setting
        /// </summary>
        internal List<string> List { get; private set; }

        /// <summary>
        /// Load SimpleListSettings from xml
        /// </summary>
        /// <param name="settings">setting xml element</param>
        public void LoadFromXml(XmlElement settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException();
            }

            this.ownerDoc = settings.OwnerDocument;
            this.List.Clear();
            XmlNode node = settings[this.ListName];

            if (node != null)
            {
                foreach (XmlNode nodeEntry in Utility.FindChildrenByName(node, this.ElementName, true))
                {
                    string text = nodeEntry.InnerText.Trim();
                    if (!string.IsNullOrEmpty(text) && !Utility.ContainsStringIgnoreCase(this.List, text))
                    {
                        this.List.Add(text);
                    }
                }
            }
        }

        /// <summary>
        /// Convert SimpleListSettings to xml
        /// </summary>
        /// <returns>xml text</returns>
        public IEnumerable<XmlElement> ToXml()
        {
            XmlElement listElement = this.ownerDoc.CreateElement(this.ListName);
            foreach (string itemName in this.List)
            {
                XmlElement itemElement = this.ownerDoc.CreateElement(this.ElementName);
                itemElement.InnerText = itemName;
                listElement.AppendChild(itemElement);
            }

            yield return listElement;
        }
    }

#pragma warning disable SA1402 // File may only contain a single class

    /// <summary>
    /// FilteringListSettings
    /// </summary>
    internal class FilteringListSettings : IDynamicCoverageSettings
#pragma warning restore SA1402 // File may only contain a single class
    {
        private const string Exclude = "Exclude";
        private const string Include = "Include";
        private const string EmptyRegex = @"^$";

        private List<string> inclusionList;
        private List<string> exclusionList;
        private XmlDocument ownerDoc;

        internal FilteringListSettings(
            XmlElement settings,
            string listName,
            string elementName,
            FilterType type,
            string sample,
            bool noneForEmptyInclude = false)
        {
            this.ListName = listName;
            this.ElementName = elementName;
            this.SampleText = sample;
            this.ListType = type;
            this.IncludeNoneForEmptyList = noneForEmptyInclude;

            this.inclusionList = new List<string>();
            this.exclusionList = new List<string>();

            this.LoadFromXml(settings);
        }

        /// <summary>
        /// Filtering type of this filter list
        /// </summary>
        internal enum FilterType
        {
            Inclusion = 0x1,
            Exclusion = 0x2,
            Both = 0x3,
        }

        /// <summary>
        /// Gets list name of this list setting
        /// </summary>
        internal string ListName { get; private set; }

        /// <summary>
        /// Gets type of this filter list
        /// </summary>
        internal FilterType ListType { get; private set; }

        /// <summary>
        /// Gets a value indicating whether whether include nothing when we have an empty include list
        /// </summary>
        internal bool IncludeNoneForEmptyList { get; private set; }

        /// <summary>
        /// Gets element name of this list setting
        /// </summary>
        internal string ElementName { get; private set; }

        /// <summary>
        /// Gets sample text
        /// </summary>
        internal string SampleText { get; private set; }

        /// <summary>
        /// Gets get inclusion list of this filering list
        /// </summary>
        internal List<string> InclusionList
        {
            get { return this.ListType != FilterType.Exclusion ? this.inclusionList : null; }
        }

        /// <summary>
        /// Gets get exclusion list of this filering list
        /// </summary>
        internal List<string> ExclusionList
        {
            get { return this.ListType != FilterType.Inclusion ? this.exclusionList : null; }
        }

        /// <summary>
        /// Verify the given XmlElement contains valid configuration,
        /// if it's invalid, XmlException will be thrown.
        /// </summary>
        /// <param name="settings">settings</param>
        /// <param name="listName">listName</param>
        /// <param name="elementName">elementName</param>
        /// <param name="type">type</param>
        public static void VerifyFilteringSetting(
            XmlElement settings,
            string listName,
            string elementName,
            FilterType type)
        {
            if (settings == null)
            {
                throw new ArgumentNullException();
            }

            FilteringListSettings newSetting =
                new FilteringListSettings(settings, listName, elementName, type, string.Empty, false);
        }

        /// <summary>
        /// Load list setting from xml
        /// </summary>
        /// <param name="settings">setting xml element</param>
        public void LoadFromXml(XmlElement settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException();
            }

            this.ownerDoc = settings.OwnerDocument;
            XmlNode listNode = settings.Name.Equals(this.ListName) ? settings : settings[this.ListName];
            if (listNode == null)
            {
                return;
            }

            VerifyFilteringItem((XmlElement)listNode, this.ListType);

            if (this.ListType != FilterType.Exclusion)
            {
                IList<XmlNode> includeNodes = this.GetIncludedItems(listNode);
                this.inclusionList.Clear();
                this.AddNodesToList(includeNodes, this.inclusionList);
            }

            if (this.ListType != FilterType.Inclusion)
            {
                IList<XmlNode> excludeNodes = this.GetExcludedItems(listNode);
                this.exclusionList.Clear();
                this.AddNodesToList(excludeNodes, this.exclusionList);
            }
        }

        /// <summary>
        /// Convert the list setting to xml
        /// </summary>
        /// <returns>xml</returns>
        public IEnumerable<XmlElement> ToXml()
        {
            XmlElement configElement = this.ownerDoc.CreateElement(this.ListName);
            if (this.ListType != FilterType.Exclusion)
            {
                configElement.AppendChild(this.GetIncludeElement());
            }

            if (this.ListType != FilterType.Inclusion)
            {
                configElement.AppendChild(this.GetExcludeElement());
            }

            yield return configElement;
        }

        /// <summary>
        /// Verify that child nodes are either Include or Exclude according to FilterType,
        /// exception will be thrown if invalid.
        /// </summary>
        /// <param name="parent">parent node</param>
        /// <param name="type">filter type.</param>
        private static void VerifyFilteringItem(XmlElement parent, FilterType type)
        {
            foreach (XmlNode node in parent.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                if (node.Name.Equals(Include))
                {
                    if (type == FilterType.Exclusion)
                    {
                        throw new XmlException(ConfigurationEditorUIResource.InvalidConfig);
                    }
                }
                else if (node.Name.Equals(Exclude))
                {
                    if (type == FilterType.Inclusion)
                    {
                        throw new XmlException(ConfigurationEditorUIResource.InvalidConfig);
                    }
                }
                else
                {
                    throw new XmlException(ConfigurationEditorUIResource.InvalidConfig);
                }
            }
        }

        private static IList<XmlNode> GetFilteringItems(XmlNode parent, string filterName, string elementName)
        {
            List<XmlNode> excludeItem = new List<XmlNode>();
            if (parent != null)
            {
                foreach (XmlNode excludeNode in Utility.FindChildrenByName(parent, filterName))
                {
                    excludeItem.AddRange(Utility.FindChildrenByName(excludeNode, elementName, true));
                }
            }

            return excludeItem;
        }

        private IList<XmlNode> GetIncludedItems(XmlNode parent)
        {
            return GetFilteringItems(parent, Include, this.ElementName);
        }

        private IList<XmlNode> GetExcludedItems(XmlNode parent)
        {
            return GetFilteringItems(parent, Exclude, this.ElementName);
        }

        private void AddNodesToList(IList<XmlNode> nodes, IList<string> list)
        {
            foreach (XmlNode nodeEntry in nodes)
            {
                string text = nodeEntry.InnerText.Trim();
                if (!string.IsNullOrEmpty(text)
                    && !Utility.ContainsStringIgnoreCase(list, text)
                    && string.Compare(text, EmptyRegex, StringComparison.Ordinal) != 0)
                {
                    // Regex verification on input filtering items
                    try
                    {
                        Regex reg = new Regex(text);
                    }
                    catch (ArgumentException)
                    {
                        // todo: allendm - currently ignoring the exception and ignoring non-regex
                        // throw new XmlException(ConfigurationEditorUIResource.InvalidRegexConfig);
                        continue;
                    }

                    list.Add(text);
                }
            }
        }

        private XmlElement GetExcludeElement()
        {
            return this.GetFilteringElement(true);
        }

        private XmlElement GetIncludeElement()
        {
            return this.GetFilteringElement(false);
        }

        private XmlElement GetFilteringElement(bool exclude)
        {
            XmlElement filtering = this.ownerDoc.CreateElement(exclude ? Exclude : Include);
            List<string> itemsToAdd = exclude ? this.ExclusionList : this.InclusionList;
            foreach (string itemName in itemsToAdd)
            {
                XmlElement itemElement = this.ownerDoc.CreateElement(this.ElementName);
                itemElement.InnerText = itemName;
                filtering.AppendChild(itemElement);
            }

            if (!exclude && this.IncludeNoneForEmptyList && this.InclusionList.Count == 0)
            {
                XmlElement itemElement = this.ownerDoc.CreateElement(this.ElementName);
                itemElement.InnerText = EmptyRegex;
                filtering.AppendChild(itemElement);
            }

            return filtering;
        }
    }
}