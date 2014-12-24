using Database.Common;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Database.Controller
{
    public class ControllerNodeSettings
    {
        private List<NodeDefinition> _connectionList = new List<NodeDefinition>();
        private string _connectionString;
        private string _name;
        private int _port;
        private int _webInterfacePort;

        public ControllerNodeSettings(XmlDocument settings)
        {
            try
            {
                var node = settings.SelectSingleNode("Settings");
                _name = node.SelectSingleNode("NodeName").InnerText;
                _port = int.Parse(node.SelectSingleNode("Port").InnerText);
                _webInterfacePort = int.Parse(node.SelectSingleNode("WebInterfacePort").InnerText);
                _connectionString = node.SelectSingleNode("ConnectionString").InnerText;
                foreach (var item in _connectionString.Split(','))
                {
                    _connectionList.Add(new NodeDefinition(item.Split(':')[0], int.Parse(item.Split(':')[1])));
                }
            }
            catch (Exception e)
            {
                Logger.Log("Exception occurred during loading of settings. Please check your settings config.\n" + e.StackTrace);
            }
        }

        public ControllerNodeSettings()
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

            var masterListElement = document.CreateElement("ConnectionString");
            rootNode.AppendChild(masterListElement);

            document.Save("masterconfig.xml");
        }

        public List<NodeDefinition> ConnectionList { get { return _connectionList; } }

        public string ConnectionString { get { return _connectionString; } }

        public string Name { get { return _name; } }

        public int Port { get { return _port; } }

        public int WebInterfacePort { get { return _webInterfacePort; } }
    }
}