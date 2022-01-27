// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.TestPlatform.Utilities
{
    /// <summary>
    /// Based on https://semver.org/
    /// </summary>
    internal class SemanticVersioning
    {
        static readonly Regex pattern = new Regex(@"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$", RegexOptions.IgnoreCase);

        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }
        public string Prerelease { get; set; }

        private string _rawValue;

        public bool IsPrerelease => !string.IsNullOrEmpty(this.Prerelease);

        private SemanticVersioning(int major, int minor, int patch, string prerelease, string rawValue)
        {
            this.Major = major;
            this.Minor = minor;
            this.Patch = patch;
            this.Prerelease = prerelease != null ? prerelease.Trim() : "";
            this._rawValue = rawValue;
        }

        public static bool TryParse(string value, out SemanticVersioning result)
        {
            result = null;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var match = pattern.Match(value);
            if (!match.Success)
            {
                return false;
            }

            result = new SemanticVersioning(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value), match.Groups[4].Value, value);

            return true;
        }

        public override string ToString() => _rawValue;
    }
}