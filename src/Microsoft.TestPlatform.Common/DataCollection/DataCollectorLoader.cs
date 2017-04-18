// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
#if !NET46
    using System.Runtime.Loader;
#endif

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <inheritdoc/>
    internal class DataCollectorLoader : IDataCollectorLoader
    {
        /// <inheritdoc/>
        public DataCollector CreateInstance(Type type)
        {
            DataCollector dataCollectorInstance = null;

            try
            {
                dataCollectorInstance = Activator.CreateInstance(type) as DataCollector;
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectorLoader.CreateInstance: Failed to load datacollector of type : {0}. Error : ", type, ex.Message);
                }
            }

            return dataCollectorInstance;
        }

        /// <inheritdoc/>
        public IEnumerable<Tuple<string, Type>> FindDataCollectors(string location)
        {
            var typeList = new List<Tuple<string, Type>>();
            if (string.IsNullOrWhiteSpace(location))
            {
                return typeList;
            }

            Assembly assembly = null;

            try
            {
#if NET46
                assembly = Assembly.LoadFrom(location);
#else
                assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(location);
#endif

                foreach (var type in assembly.GetTypes())
                {
                    if (type.GetTypeInfo().IsSubclassOf(typeof(DataCollector)) && !type.GetTypeInfo().IsAbstract)
                    {
                        var friendlyName = (DataCollectorFriendlyNameAttribute)GetAttributes(type, typeof(DataCollectorFriendlyNameAttribute)).FirstOrDefault();
                        if (friendlyName != null)
                        {
                            typeList.Add(new Tuple<string, Type>(friendlyName.FriendlyName, type));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (EqtTrace.IsErrorEnabled)
                {
                    EqtTrace.Error("DataCollectorLoader.FindDataCollectors: Failed to find datacollectors from assembly at location : {0}. Error {1}: ", location, ex.Message);
                }
            }

            return typeList;
        }

        /// <inheritdoc/>
        public Uri GetTypeUri(Type dataCollectorType)
        {
            ValidateArg.NotNull(dataCollectorType, nameof(dataCollectorType));

            DataCollectorTypeUriAttribute typeUriAttribute = null;
            try
            {
                var typeUriAttributes = GetAttributes(dataCollectorType, typeof(DataCollectorTypeUriAttribute));
                typeUriAttribute = (DataCollectorTypeUriAttribute)typeUriAttributes[0];
            }
            catch (Exception)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Resources.DataCollector_TypeIsNull, dataCollectorType.FullName));
            }

            // The type uri can not be null or empty.
            if (string.IsNullOrEmpty(typeUriAttribute.TypeUri))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Resources.DataCollector_TypeIsNull, dataCollectorType.FullName));
            }

            return new Uri(typeUriAttribute.TypeUri);
        }

        /// <inheritdoc/>
        public string GetFriendlyName(Type dataCollectorType)
        {
            ValidateArg.NotNull(dataCollectorType, nameof(dataCollectorType));

            DataCollectorFriendlyNameAttribute friendlyNameAttribute = null;

            // Get the friendly name from the attribute.
            try
            {
                var friendlyNameAttributes = GetAttributes(dataCollectorType, typeof(DataCollectorFriendlyNameAttribute));
                friendlyNameAttribute = (DataCollectorFriendlyNameAttribute)friendlyNameAttributes[0];
            }
            catch (Exception)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Resources.FriendlyNameIsNullOrEmpty, dataCollectorType.FullName));
            }

            // Verify that the friendly name provided is not null or empty.
            if (string.IsNullOrEmpty(friendlyNameAttribute.FriendlyName))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Resources.FriendlyNameIsNullOrEmpty, dataCollectorType.FullName));
            }

            return friendlyNameAttribute.FriendlyName;
        }

        /// <summary>
        /// Gets the attributes of the specified type from the data collector type.
        /// </summary>
        /// <param name="dataCollectorType">
        /// Data collector type to get attribute from.
        /// </param>
        /// <param name="attributeType">
        /// The type of attribute to look for.
        /// </param>
        /// <returns>
        /// Array of attributes matching the type provided.  Will be an empty array if none were found.
        /// </returns>
        private static object[] GetAttributes(Type dataCollectorType, Type attributeType)
        {
            ValidateArg.NotNull(dataCollectorType, nameof(dataCollectorType));
            ValidateArg.NotNull(attributeType, nameof(attributeType));

            // If any attribute constructor on the type throws, the exception will bubble up through
            // the "GetCustomAttributes" method.
            return dataCollectorType.GetTypeInfo().GetCustomAttributes(attributeType, true).ToArray<object>();
        }
    }
}