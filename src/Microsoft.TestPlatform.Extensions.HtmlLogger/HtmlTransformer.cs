using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
namespace Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger
{
    class HtmlTransformer : IHtmlTransformer
    {
        StringReader xsltStringReader = new StringReader(Resources.Html);
        XslCompiledTransform myXslTransform;

        public HtmlTransformer()
        {
            myXslTransform = new XslCompiledTransform();
            myXslTransform.Load(XmlReader.Create(xsltStringReader));
        }

        public void Transform(string xmlfile,string htmlfile)
        {
            myXslTransform.Transform(xmlfile, htmlfile);
        }
    }
}
