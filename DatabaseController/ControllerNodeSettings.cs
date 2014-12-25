using Database.Common;
using System.Globalization;
using System.Xml;

namespace Database.Controller
{
    public class ControllerNodeSettings : Settings
    {
        private string _connectionString;
        private int _maxChunkItemCount = 1000;

        private int _maxChunkSize = 64 * 1024;

        // Max size of 64kb
        private int _port = 12345;

        private int _redundentNodesPerLocation = 3;

        private int _webInterfacePort = 12346;

        public ControllerNodeSettings(string xml)
            : base(xml)
        {
        }

        public ControllerNodeSettings()
            : base()
        {
        }

        public string ConnectionString { get { return _connectionString; } }

        public int MaxChunkItemCount { get { return _maxChunkItemCount; } }

        public int MaxChunkSize { get { return _maxChunkSize; } }

        public int Port { get { return _port; } }

        public int RedundentNodesPerLocation { get { return _redundentNodesPerLocation; } }

        public int WebInterfacePort { get { return _webInterfacePort; } }

        protected override void Load(XmlNode settings)
        {
            _connectionString = settings.SelectSingleNode("ConnectionString").InnerText;
            _port = int.Parse(settings.SelectSingleNode("Port").InnerText);
            _webInterfacePort = int.Parse(settings.SelectSingleNode("WebInterfacePort").InnerText);
            _maxChunkSize = int.Parse(settings.SelectSingleNode("MaxChunkSize").InnerText);
            _maxChunkItemCount = int.Parse(settings.SelectSingleNode("MaxChunkItemCount").InnerText);
            _redundentNodesPerLocation = int.Parse(settings.SelectSingleNode("RedundentNodesPerLocation").InnerText);
        }

        protected override void Save(XmlDocument document, XmlNode root)
        {
            var connectionStringNode = document.CreateElement("ConnectionString");
            connectionStringNode.InnerText = _connectionString;
            root.AppendChild(connectionStringNode);

            var portNode = document.CreateElement("Port");
            portNode.InnerText = _port.ToString(CultureInfo.InvariantCulture);
            root.AppendChild(portNode);

            var webInterfacePortNode = document.CreateElement("WebInterfacePort");
            webInterfacePortNode.InnerText = _webInterfacePort.ToString(CultureInfo.InvariantCulture);
            root.AppendChild(webInterfacePortNode);

            var maxChunkSizeNode = document.CreateElement("MaxChunkSize");
            maxChunkSizeNode.InnerText = _maxChunkSize.ToString(CultureInfo.InvariantCulture);
            root.AppendChild(maxChunkSizeNode);

            var maxChunkItemCountNode = document.CreateElement("MaxChunkItemCount");
            maxChunkItemCountNode.InnerText = _maxChunkItemCount.ToString(CultureInfo.InvariantCulture);
            root.AppendChild(maxChunkItemCountNode);

            var redundentNodesPerLocation = document.CreateElement("RedundentNodesPerLocation");
            redundentNodesPerLocation.InnerText = _redundentNodesPerLocation.ToString(CultureInfo.InvariantCulture);
            root.AppendChild(redundentNodesPerLocation);
        }
    }
}