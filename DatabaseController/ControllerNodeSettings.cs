using Database.Common;
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
            _connectionString = ReadString(settings, "ConnectionString", _connectionString);
            _port = ReadInt32(settings, "Port", _port);
            _webInterfacePort = ReadInt32(settings, "WebInterfacePort", _webInterfacePort);
            _maxChunkSize = ReadInt32(settings, "MaxChunkSize", _maxChunkSize);
            _maxChunkItemCount = ReadInt32(settings, "MaxChunkItemCount", _maxChunkItemCount);
            _redundentNodesPerLocation = ReadInt32(settings, "RedundentNodesPerLocation", _redundentNodesPerLocation);
        }

        protected override void Save(XmlDocument document, XmlNode root)
        {
            WriteString(document, "ConnectionString", _connectionString, root);
            WriteInt32(document, "Port", _port, root);
            WriteInt32(document, "WebInterfacePort", _webInterfacePort, root);
            WriteInt32(document, "MaxChunkSize", _maxChunkSize, root);
            WriteInt32(document, "MaxChunkItemCount", _maxChunkItemCount, root);
            WriteInt32(document, "RedundentNodesPerLocation", _redundentNodesPerLocation, root);
        }
    }
}