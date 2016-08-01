// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Class that holds Trait. 
    /// A traits is Name, Value pair.
    /// </summary>
#if NET46
    [Serializable]
#endif
    public class Trait
    {
        public string Name { get; private set; }
        public string Value { get; private set; }

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
