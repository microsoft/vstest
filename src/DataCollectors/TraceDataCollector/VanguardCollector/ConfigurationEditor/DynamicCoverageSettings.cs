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

    /// <summary>
    /// Interface for all DynamicCoverageSetting
    /// </summary>
    internal interface IDynamicCoverageSettings
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
        private const char semicolon = ';';

        /// <summary>
        /// Quote
        /// </summary>
        private const char quote = '"';

        /// <summary>
        /// XmlNode.SelectSingleNode and XmlNode.SelectNodes is too heavy when element has xmlns
        /// FindChildrenByName is used to replace SelectNodes when we need to select children by name
        /// </summary>
        /// <param name="parent">parent node</param>
        /// <param name="name">element name to search</param>
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
                if (String.Compare(item, str, StringComparison.OrdinalIgnoreCase) == 0)
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
                if (i == str.Length || str[i] == semicolon)
                {
                    if (!insideQuote)
                    {
                        string item = str.Substring(begin, i - begin).Trim().Replace("\"", "");
                        if (!string.IsNullOrEmpty(item))
                        {
                            yield return item;
                        }

                        begin = i + 1;
                    }
                }
                else if (str[i] == quote)
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
                    builder.Append(semicolon);
                }

                first = false;
                if (str.Contains(quote))
                {
                    // Quotes are not allowed in input strings.
                    throw new ArgumentException(null, "list");
                }
                if (str.Contains(semicolon))
                {
                    // If string contains semicolon, surround it with quotes.
                    builder.Append(quote).Append(str).Append(quote);
                }
                else
                {
                    builder.Append(str);
                }
            }

            return builder.ToString();
        }
    }

    /// <summary>
    /// DynamicCoverageAdvancedSettings
    /// </summary>
    internal class DynamicCoverageAdvancedSettings : IDynamicCoverageSettings
    {
        Dictionary<string, object> generalSettings = new Dictionary<string, object> 
                                                        { 
                                                            { CollectChildProcessStr, true },
                                                            { UseVerifiableInstrumentationStr, true },
                                                            { AllowLowIntegrityProcessStr, true },
                                                            { ExcludeCompilerAutoGeneratedModulesStr, true},
                                                        };
        private XmlDocument ownerDoc = new XmlDocument();

        private const string CollectAspDotNetStr = "CollectAspDotNet";
        private const string CollectChildProcessStr = "CollectFromChildProcesses";
        private const string UseVerifiableInstrumentationStr = "UseVerifiableInstrumentation";
        private const string AllowLowIntegrityProcessStr = "AllowLowIntegrityProcesses";

        // We do not want to show code coverage for modules which are auto-generated by the compiler
        // Modules generated by ASP.NET for web pages(.aspx) come under this category.
        private const string ExcludeCompilerAutoGeneratedModulesStr = "ExcludeCompilerAutoGeneratedModules";

        internal const string AllowedUserListName = "AllowedUsers";
        internal const string AllowedUserElementName = "User";
        internal const string Everyone = "Everyone";

        private const string SymbolPathListName = "SymbolSearchPaths";
        private const string SymbolPathElementName = "Path";

        private const string CompanyNameListName = "CompanyNames";
        private const string CompanyNameElementName = "CompanyName";
        private const string CompanyNameSample = @".*microsoft.*";
        private const string PublicKeyTokenListName = "PublicKeyTokens";
        private const string PublicKeyTokenElementName = "PublicKeyToken";
        private const string PublickeyTokenSample = "^1234$";

        /// <summary>
        /// UseVerifiableInstrumentation setting
        /// </summary>
        internal bool UseVerifiableInstrumentation
        {
            get
            {
                return GetSetting<bool>(UseVerifiableInstrumentationStr, generalSettings);
            }

            set
            {
                UpdateSetting(UseVerifiableInstrumentationStr, value, generalSettings);
            }
        }

        /// <summary>
        /// AllowLowIntegrityProcess setting
        /// </summary>
        internal bool AllowLowIntegrityProcess
        {
            get
            {
                return GetSetting<bool>(AllowLowIntegrityProcessStr, generalSettings);
            }

            set
            {
                UpdateSetting(AllowLowIntegrityProcessStr, value, generalSettings);
            }
        }

        /// <summary>
        /// CollectChildProcess setting
        /// </summary>
        internal bool CollectChildProcess
        {
            get
            {
                return GetSetting<bool>(CollectChildProcessStr, generalSettings);
            }

            set
            {
                UpdateSetting(CollectChildProcessStr, value, generalSettings);
            }
        }

        /// <summary>
        /// Allowed user setting
        /// </summary>
        internal SimpleListSettings AllowedUserList
        {
            get;
            private set;
        }

        /// <summary>
        /// SymbolPathList setting
        /// </summary>
        internal SimpleListSettings SymbolPathList
        {
            get;
            private set;
        }

        /// <summary>
        /// CompanyNameList setting
        /// </summary>
        internal FilteringListSettings CompanyNameList { get; set; }

        /// <summary>
        /// PublicKeyTokenList setting
        /// </summary>
        internal FilteringListSettings PublicKeyTokenList { get; set; }

        internal DynamicCoverageAdvancedSettings(XmlElement settings)
        {
            Debug.Assert(settings != null);

            SymbolPathList = new SimpleListSettings(settings, SymbolPathListName, SymbolPathElementName);
            AllowedUserList = new SimpleListSettings(settings, AllowedUserListName, AllowedUserElementName);

            CompanyNameList = new FilteringListSettings(settings, CompanyNameListName, CompanyNameElementName,
                                                       FilteringListSettings.FilterType.Exclusion, CompanyNameSample, true);

            PublicKeyTokenList = new FilteringListSettings(settings, PublicKeyTokenListName, PublicKeyTokenElementName,
                                                       FilteringListSettings.FilterType.Exclusion, PublickeyTokenSample, true);

            LoadGeneralSettingFromXml(settings, ref generalSettings, ref ownerDoc);
        }

        /// <summary>
        /// Load DynamicCoverageAdvancedSettings from xml
        /// </summary>
        /// <param name="element">setting xml element</param>
        public void LoadFromXml(XmlElement settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException();
            }

            LoadGeneralSettingFromXml(settings, ref generalSettings, ref ownerDoc);
            SymbolPathList.LoadFromXml(settings);
            AllowedUserList.LoadFromXml(settings);
            CompanyNameList.LoadFromXml(settings);
            PublicKeyTokenList.LoadFromXml(settings);
        }

        /// <summary>
        /// Convert DynamicCoverageAdvancedSettings to xml
        /// </summary>
        /// <returns>xml</returns>
        public IEnumerable<XmlElement> ToXml()
        {
            foreach (string settingName in generalSettings.Keys)
            {
                XmlElement settingElement = ownerDoc.CreateElement(settingName);
                settingElement.InnerText = generalSettings[settingName].ToString();
                yield return settingElement;
            }

            foreach (XmlElement element in SymbolPathList.ToXml())
            {
                yield return element;
            }

            foreach (XmlElement element in AllowedUserList.ToXml())
            {
                yield return element;
            }

            foreach (XmlElement element in CompanyNameList.ToXml())
            {
                yield return element;
            }

            foreach (XmlElement element in PublicKeyTokenList.ToXml())
            {
                yield return element;
            }
        }
        
        internal static void LoadGeneralSettingFromXml(XmlElement settings, ref Dictionary<string, object> generalSettings, 
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
            return (generalSettings.ContainsKey(settingName));
        }

        internal static void UpdateSetting<T>(string settingName, T value, Dictionary<string, object> generalSettings)
        {
            generalSettings[settingName] = value;
        }
    }

    /// <summary>
    /// DynamicCoverageModuleSettings
    /// </summary>
    internal class DynamicCoverageModuleSettings : IDynamicCoverageSettings
    {
        Dictionary<string, object> generalSettings = new Dictionary<string, object> { { CollectAspDotNetStr, false } };
        XmlDocument ownerDoc = new XmlDocument();

        private const string ModulePathListName = "ModulePaths";
        internal const string ModulePathElementName = "ModulePath";
        private const string ModulePathSample = @"*\VC\redist\*";
        internal const string EntryPointListName = "EntryPoints";
        internal const string EntryPointElementName = "EntryPoint";
        internal const string EntryPointSample = "sample.exe";
        private const string CollectAspDotNetStr = "CollectAspDotNet";

        /// <summary>
        /// ModulePathList setting
        /// </summary>
        internal FilteringListSettings ModulePathList
        {
            get;
            private set;
        }

        /// <summary>
        /// Entry point setting
        /// </summary>
        internal SimpleListSettings EntryPointList
        {
            get;
            private set;
        }

        /// <summary>
        /// CollectAspDotNet setting
        /// </summary>
        internal bool CollectAspDotNet
        {
            get
            {
                return DynamicCoverageAdvancedSettings.GetSetting<bool>(CollectAspDotNetStr, generalSettings);
            }
            set
            {
                DynamicCoverageAdvancedSettings.UpdateSetting(CollectAspDotNetStr, value, generalSettings);
            }
        }

        internal DynamicCoverageModuleSettings(XmlElement settings, bool loadAll = true)
        {
            Debug.Assert(settings != null);
            if (loadAll)
            {
                ModulePathList = new FilteringListSettings(settings, ModulePathListName, ModulePathElementName,
                                                           FilteringListSettings.FilterType.Inclusion, ModulePathSample, true);
                EntryPointList = new SimpleListSettings(settings, EntryPointListName, EntryPointElementName);
            }

            DynamicCoverageAdvancedSettings.LoadGeneralSettingFromXml(settings, ref generalSettings, ref ownerDoc);
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

            DynamicCoverageAdvancedSettings.LoadGeneralSettingFromXml(settings, ref generalSettings, ref ownerDoc);
            ModulePathList.LoadFromXml(settings);
            EntryPointList.LoadFromXml(settings);
        }

        /// <summary>
        /// Convert DynamicCoverageModuleSettings to xml
        /// </summary>
        /// <returns>xml</returns>
        public IEnumerable<XmlElement> ToXml()
        {
            foreach (string settingName in generalSettings.Keys)
            {
                XmlElement settingElement = ownerDoc.CreateElement(settingName);
                settingElement.InnerText = generalSettings[settingName].ToString();
                yield return settingElement;
            }

            foreach (XmlElement element in ModulePathList.ToXml())
            {
                yield return element;
            }

            foreach (XmlElement element in EntryPointList.ToXml())
            {
                yield return element;
            }
        }
    }

    /// <summary>
    /// DynamicCoverageReadOnlySettings
    /// </summary>
    internal class DynamicCoverageReadOnlySettings : IDynamicCoverageSettings
    {
        private static readonly string[] FunctionSig = { "Functions", "Function" };
        private static readonly string[] FunctionAttr = { "Attributes", "Attribute" };
        private static readonly string[] Source = { "Sources", "Source" };
        private static readonly string[] Company = { "CompanyNames", "CompanyName" };
        private static readonly string[] ModulePKT = { "PublicKeyTokens", "PublicKeyToken" };
        private static readonly string[][] ListNames = { FunctionSig, FunctionAttr, Source };
        
        private static List<NameElementPair> readOnlySettings;

        private class NameElementPair
        {
            internal NameElementPair(string listName, string elementName)
            {
                ListName = listName;
                ElementName = elementName;
            }
            internal string ElementName { get; private set; }
            internal string ListName { get; private set; }
            internal XmlElement Config { get; set; }
        }

        /// <summary>
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
        /// DynamicCoverageReadOnlySettings Constructor
        /// </summary>
        /// <param name="settings">settings</param>
        internal DynamicCoverageReadOnlySettings(XmlElement settings)
        {
            Debug.Assert(settings != null);

            LoadFromXml(settings);
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
                    FilteringListSettings.VerifyFilteringSetting(configElement, pair.ListName, pair.ElementName, FilteringListSettings.FilterType.Both);
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
    }

    /// <summary>
    /// SimpleListSettings
    /// </summary>
    internal class SimpleListSettings : IDynamicCoverageSettings
    {
        private XmlDocument ownerDoc;

        /// <summary>
        /// List name of this list setting
        /// </summary>
        internal string ListName
        {
            private set;
            get;
        }

        /// <summary>
        /// Element name of this list setting
        /// </summary>
        internal string ElementName
        {
            private set;
            get;
        }

        /// <summary>
        /// Content of this list setting
        /// </summary>
        internal List<string> List
        {
            get;
            private set;
        }

        internal SimpleListSettings(XmlElement settings, string listName, string elementName)
        {
            Debug.Assert(settings != null);
            Debug.Assert(!string.IsNullOrEmpty(listName));
            Debug.Assert(!string.IsNullOrEmpty(elementName));

            ListName = listName;
            ElementName = elementName;
            List = new List<string>();
            
            LoadFromXml(settings);
        }

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

            ownerDoc = settings.OwnerDocument;
            List.Clear();
            XmlNode node = settings[ListName];
            
            if (node != null)
            {
                foreach (XmlNode nodeEntry in Utility.FindChildrenByName(node, ElementName, true))
                {
                    string text = nodeEntry.InnerText.Trim();
                    if (!string.IsNullOrEmpty(text) && !Utility.ContainsStringIgnoreCase(List, text))
                    {
                        List.Add(text);
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
            XmlElement listElement = ownerDoc.CreateElement(ListName);
            foreach (string itemName in List)
            {
                XmlElement itemElement = ownerDoc.CreateElement(ElementName);
                itemElement.InnerText = itemName;
                listElement.AppendChild(itemElement);
            }

            yield return listElement;
        }
    }

    /// <summary>
    /// FilteringListSettings
    /// </summary>
    internal class FilteringListSettings : IDynamicCoverageSettings
    {
        private List<string> inclusionList;
        private List<string> exclusionList;
        private XmlDocument ownerDoc;
        
        private const string Exclude = "Exclude";
        private const string Include = "Include";

        private const string EmptyRegex = @"^$";

        /// <summary>
        /// List name of this list setting
        /// </summary>
        internal string ListName
        {
            private set;
            get;
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
        /// Type of this filter list
        /// </summary>
        internal FilterType ListType
        {
            private set;
            get;
        }

        /// <summary>
        /// Whether include nothing when we have an empty include list
        /// </summary>
        internal bool IncludeNoneForEmptyList
        {
            private set;
            get;
        }

        /// <summary>
        /// Element name of this list setting
        /// </summary>
        internal string ElementName
        {
            private set;
            get;
        }

        /// <summary>
        /// Sample text
        /// </summary>
        internal string SampleText
        {
            private set;
            get;
        }

        /// <summary>
        /// Get inclusion list of this filering list
        /// </summary>
        internal List<string> InclusionList
        {
            get 
            {
                return ListType != FilterType.Exclusion ? inclusionList : null; 
            }
        }

        /// <summary>
        /// Get exclusion list of this filering list
        /// </summary>
        internal List<string> ExclusionList
        {
            get
            {
                return ListType != FilterType.Inclusion ? exclusionList: null; 
            }
        }

        internal FilteringListSettings(XmlElement settings, string listName, string elementName, FilterType type, string sample, bool noneForEmptyInclude = false)
        {
            Debug.Assert(settings != null);
            Debug.Assert(!string.IsNullOrEmpty(listName));
            Debug.Assert(!string.IsNullOrEmpty(elementName));

            ListName = listName;
            ElementName = elementName;
            SampleText = sample;
            ListType = type;
            IncludeNoneForEmptyList = noneForEmptyInclude;

            inclusionList = new List<string>();
            exclusionList = new List<string>();

            LoadFromXml(settings);
        }

        /// <summary>
        /// Verify the given XmlElement contains valid configuration, 
        /// if it's invalid, XmlException will be thrown.
        /// </summary>
        /// <param name="settings">settings</param>
        /// <param name="listName">listName</param>
        /// <param name="elementName">elementName</param>
        /// <param name="type">type</param>
        public static void VerifyFilteringSetting(XmlElement settings, string listName, string elementName, FilterType type)
        {
            if (settings == null)
            {
                throw new ArgumentNullException();
            }
            
            FilteringListSettings newSetting = new FilteringListSettings(settings, listName, elementName, type, string.Empty, false);
        }

        /// <summary>
        /// Verify that child nodes are either Include or Exclude according to FilterType,
        /// exception will be thrown if invalid.
        /// </summary>
        /// <param name="parent">parent node</param>
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

            ownerDoc = settings.OwnerDocument;
            XmlNode listNode = settings.Name.Equals(ListName) ? settings : settings[ListName];
            if (listNode == null)
            {
                return;
            }

            VerifyFilteringItem((XmlElement)listNode, ListType);

            if (ListType != FilterType.Exclusion)
            {
                IList<XmlNode> includeNodes = GetIncludedItems(listNode);
                inclusionList.Clear();
                AddNodesToList(includeNodes, inclusionList);
            }

            if (ListType != FilterType.Inclusion)
            {
                IList<XmlNode> excludeNodes = GetExcludedItems(listNode);
                exclusionList.Clear();
                AddNodesToList(excludeNodes, exclusionList);
            }
        }

        /// <summary>
        /// Convert the list setting to xml
        /// </summary>
        /// <returns>xml</returns>
        public IEnumerable<XmlElement> ToXml()
        {
            XmlElement configElement = ownerDoc.CreateElement(ListName);
            if (ListType != FilterType.Exclusion)
            {
                configElement.AppendChild(GetIncludeElement());
            }
            if (ListType != FilterType.Inclusion)
            {
                configElement.AppendChild(GetExcludeElement());
            }

            yield return configElement;
        }

        private IList<XmlNode> GetIncludedItems(XmlNode parent)
        {
            return GetFilteringItems(parent, Include, ElementName);
        }

        private IList<XmlNode> GetExcludedItems(XmlNode parent)
        {
            return GetFilteringItems(parent, Exclude, ElementName);
        }

        private static IList<XmlNode> GetFilteringItems(XmlNode parent, string filterName, string elementName)
        {
            Debug.Assert(parent != null);

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

        private void AddNodesToList(IList<XmlNode> nodes, IList<string> list)
        {
            foreach (XmlNode nodeEntry in nodes)
            {
                string text = nodeEntry.InnerText.Trim();
                if (!string.IsNullOrEmpty(text) 
                    && !Utility.ContainsStringIgnoreCase(list, text) 
                    && String.Compare(text, EmptyRegex, StringComparison.Ordinal) != 0)
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
            return GetFilteringElement(true);
        }

        private XmlElement GetIncludeElement()
        {
            return GetFilteringElement(false);
        }

        private XmlElement GetFilteringElement(bool exclude)
        {
            XmlElement filtering = ownerDoc.CreateElement(exclude ? Exclude : Include);
            List<string> itemsToAdd = exclude ? ExclusionList : InclusionList;
            foreach (string itemName in itemsToAdd)
            {
                XmlElement itemElement = ownerDoc.CreateElement(ElementName);
                itemElement.InnerText = itemName;
                filtering.AppendChild(itemElement);
            }

            if (!exclude && IncludeNoneForEmptyList && InclusionList.Count == 0)
            {
                XmlElement itemElement = ownerDoc.CreateElement(ElementName);
                itemElement.InnerText = EmptyRegex;
                filtering.AppendChild(itemElement);
            }

            return filtering;
        }
    }
}
