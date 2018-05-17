using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine
{
    public class TestSettingsNodes
    {
        public XmlNode Deployment { get; set; }
        public XmlNode Script { get; set; }
        public XmlNode WebSettings { get; set; }
        public XmlNodeList Datacollectors { get; set; }
        public XmlNode Timeout { get; set; }
        public XmlNode UnitTestConfig { get; set; }
        public XmlNode Hosts { get; set; }
        public XmlNode Execution { get; set; }
    }
}
