// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Text;

    /// <summary> 
    /// Filter options to be passed into the Test Platform during Discovery/Execution. 
    /// </summary> 
    [DataContract]
    public class FilterOptions
    {
        /// <summary> 
        /// Gets or sets the regular expression that will be applied on the property before matching. 
        /// </summary> 
        [DataMember]
        public string FilterRegEx { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "FilterOptions:"));
            sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "   FilterRegEx={0}", this.FilterRegEx ?? string.Empty));
            return sb.ToString();
        }

        protected bool Equals(FilterOptions other) => 
            other != null && string.Equals(this.FilterRegEx, other.FilterRegEx);

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as FilterOptions);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.FilterRegEx.GetHashCode();
        }
    }
}
