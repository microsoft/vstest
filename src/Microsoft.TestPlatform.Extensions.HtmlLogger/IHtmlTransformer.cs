// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger
{
    /// <summary>
    /// 
    /// </summary>
    public interface IHtmlTransformer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmlfile"></param>
        /// <param name="htmlfile"></param>
        void Transform(string xmlfile, string htmlfile);
    }
}