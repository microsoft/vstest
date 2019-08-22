// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger
{
    using System.Xml;
    using System.Xml.Xsl;

    class HtmlTransformer : IHtmlTransformer
    {
        XslCompiledTransform xslTransform;

        /// <summary>
        /// The following function invokes the compiled tranform and Loads the xslt file.
        /// </summary>
        public HtmlTransformer()
        {
            xslTransform = new XslCompiledTransform();
            xslTransform.Load(XmlReader.Create(this.GetType().Assembly.GetManifestResourceStream("Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger.Html.xslt")));
        }

        /// <summary>
        /// It transforms the xmlfile to htmlfile.
        /// </summary>
        public void Transform(string xmlfile, string htmlfile)
        {
            xslTransform.Transform(xmlfile, htmlfile);
        }
    }
}
