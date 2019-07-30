// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger
{
    public interface IHtmlTransformer
    {
        /// <summary>
        /// It transforms the xmlfile to htmlfile 
        /// </summary>
        /// <param name="xmlfile"></param>
        /// <param name="htmlfile"></param>
        void Transform(string xmlfile, string htmlfile);
    }
}