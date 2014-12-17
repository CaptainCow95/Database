using Database.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Database.Master
{
    public class MasterNodeSettings
    {
        private List<MasterNodeDefinition> _masterList = new List<MasterNodeDefinition>();
        private string _name;
        private int _port;
        private int _webInterfacePort;

        public MasterNodeSettings(XmlDocument settings)
        {
            try
            {
                var node = settings.SelectSingleNode("Settings");
                _name = node.SelectSingleNode("NodeName").InnerText;
                _port = int.Parse(node.SelectSingleNode("Port").InnerText);
                _webInterfacePort = int.Parse(node.SelectSingleNode("WebInterfacePort").InnerText);
                var masterListNode = node.SelectSingleNode("MasterList");
                foreach (var item in masterListNode.SelectNodes("Node").Cast<XmlNode>())
                {
                    string nodeName = item.SelectSingleNode("NodeName").InnerText;
                    string hostname = item.SelectSingleNode("Hostname").InnerText;
                    int port = int.Parse(item.SelectSingleNode("Port").InnerText);
                    _masterList.Add(new MasterNodeDefinition(nodeName, hostname, port));
                }
            }
            catch (Exception e)
            {
                Logger.Log("Exception occurred during loading of settings. Please check your settings config.\n" + e.StackTrace);
            }
        }

        public MasterNodeSettings()
        {
            _name = "Master Node";
            _port = 12345;
            _webInterfacePort = _port + 1;

            XmlDocument document = new XmlDocument();

            var rootNode = document.CreateElement("Settings");
            document.AppendChild(rootNode);

            var nodeNameElement = document.CreateElement("NodeName");
            nodeNameElement.AppendChild(document.CreateTextNode(_name));
            rootNode.AppendChild(nodeNameElement);

            var portElement = document.CreateElement("Port");
            portElement.AppendChild(document.CreateTextNode(_port.ToString()));
            rootNode.AppendChild(portElement);

            var webInterfacePortElement = document.CreateElement("WebInterfacePort");
            webInterfacePortElement.AppendChild(document.CreateTextNode(_webInterfacePort.ToString()));
            rootNode.AppendChild(webInterfacePortElement);

            var masterListElement = document.CreateElement("MasterList");
            rootNode.AppendChild(masterListElement);

            document.Save("masterconfig.xml");
        }

        public List<MasterNodeDefinition> MasterList { get { return _masterList; } }

        public string Name { get { return _name; } }

        public int Port { get { return _port; } }

        public int WebInterfacePort { get { return _webInterfacePort; } }
    }
}