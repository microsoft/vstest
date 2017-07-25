// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Class that holds Trait. 
    /// A traits is Name, Value pair.
    /// </summary>
#if NET451
    [Serializable]
#endif
    [DataContract]
    public class Trait
    {
        [DataMember(Name = "Key")]
        public string Name { get; set; }

        [DataMember(Name = "Value")]
        public string Value { get; set; }

        internal Trait(KeyValuePair<string, string> data)
            : this(data.Key, data.Value)
        {
        }

        public Trait(string name, string value)
        {
            ValidateArg.NotNull(name, "name");

            this.Name = name;
            this.Value = value;
        }
    }
}
