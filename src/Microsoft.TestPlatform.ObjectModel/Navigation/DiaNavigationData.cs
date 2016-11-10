// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel
{


    using System.Diagnostics.CodeAnalysis;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Navigation;
    
    /// <summary>
    /// A struct that stores the infomation needed by the navigation: file name, line number, column number.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Dia is a specific name.")]
    public class DiaNavigationData : INavigationData
    {
        public string FileName { get; set; }

        public int MinLineNumber { get; set; }

        public int MaxLineNumber { get; set; }

        public DiaNavigationData(string fileName, int minLineNumber, int maxLineNumber)
        {
            this.FileName = fileName;
            this.MinLineNumber = minLineNumber;
            this.MaxLineNumber = maxLineNumber;
        }
    }

}