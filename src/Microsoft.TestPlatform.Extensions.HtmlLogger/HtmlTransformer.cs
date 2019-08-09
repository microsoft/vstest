// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger
{
    using System.IO;
    using System.Xml;
    using System.Xml.Xsl;
    using HtmlResources = Resources.Resources;

    class HtmlTransformer : IHtmlTransformer
    {
        StringReader xsltStringReader = new StringReader(HtmlResources.Html);
        XslCompiledTransform myXslTransform;

        /// <summary>
        /// The following function invokes the compiled tranform and Loads the xslt file.
        /// </summary>
        public HtmlTransformer()
        {
            myXslTransform = new XslCompiledTransform();
            myXslTransform.Load(XmlReader.Create(xsltStringReader));
        }

        /// <summary>
        /// It transforms the xmlfile to htmlfile.
        /// </summary>
        public void Transform(string xmlfile, string htmlfile)
        {
            myXslTransform.Transform(xmlfile, htmlfile);
        }
    }
}
