// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Class that holds Trait. 
    /// A traits is Name, Value pair.
    /// </summary>
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
