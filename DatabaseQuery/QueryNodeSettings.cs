using Database.Common;
using System.Xml;

namespace Database.Query
{
    public class QueryNodeSettings : Settings
    {
        private string _connectionString;
        private string _nodeName = "localhost";
        private int _port = 12345;

        public QueryNodeSettings(string xml)
            : base(xml)
        {
        }

        public QueryNodeSettings()
            : base()
        {
        }

        public string ConnectionString { get { return _connectionString; } }

        public string NodeName { get { return _nodeName; } }

        public int Port { get { return _port; } }

        protected override void Load(XmlNode settings)
        {
            _connectionString = ReadString(settings, "ConnectionString", _connectionString);
            _nodeName = ReadString(settings, "NodeName", _nodeName);
            _port = ReadInt32(settings, "Port", _port);
        }

        protected override void Save(XmlDocument document, XmlNode root)
        {
            WriteString(document, "ConnectionString", _connectionString, root);
            WriteString(document, "NodeName", _nodeName, root);
            WriteInt32(document, "Port", _port, root);
        }
    }
}