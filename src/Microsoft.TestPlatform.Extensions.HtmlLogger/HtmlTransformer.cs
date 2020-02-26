// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger
{
    using System.Xml;
    using System.Xml.Xsl;

    /// <summary>
    /// Html transformer transforms the xml file to html file using xslt file.
    /// </summary>
    internal class HtmlTransformer : IHtmlTransformer
    {
        private readonly XslCompiledTransform xslTransform;

        /// <summary>
        /// The following function invokes the compiled transform and Loads the xslt file.
        /// </summary>
        public HtmlTransformer()
        {
            xslTransform = new XslCompiledTransform();
            xslTransform.Load(XmlReader.Create(this.GetType().Assembly.GetManifestResourceStream("Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger.Html.xslt") ?? throw new InvalidOperationException()));
        }

        /// <summary>
        /// It transforms the xml file to html file.
        /// </summary>
        public void Transform(string xmlFile, string htmlFile)
        {
            xslTransform.Transform(xmlFile, htmlFile);
        }
    }
}
